namespace Armoire.Features.Penumbra.Core;

using Armoire.Features.LocalScanner;
using Armoire.Features.Penumbra.Core.Domain;
using Armoire.Features.Penumbra.Core.Models;
using Armoire.Features.Penumbra.Interfaces;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

public class PenumbraRepository : IPenumbraRepository, IDisposable {
    private readonly IPluginLog pluginLog;
    private readonly IModScannerManager modScannerManager;
    private readonly Dictionary<string, PenumbraCollection> collectionCache = [];
    private readonly FileSystemWatcher? watcher;

    private bool isLoading = false;
    private bool isStale = true;

    public bool IsLoading => isLoading;
    public bool IsStale => isStale;

    public PenumbraRepository(IPluginLog pluginLog, IModScannerManager modScannerManager) {
        this.pluginLog = pluginLog;
        this.modScannerManager = modScannerManager;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var collectionsDir = Path.Combine(appData, "XIVLauncher", "pluginConfigs", "Penumbra", "collections");

        if (Directory.Exists(collectionsDir)) {
            this.watcher = new FileSystemWatcher(collectionsDir, "*.json") {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            this.watcher.Changed += OnCollectionFileChanged;
            this.watcher.Created += OnCollectionFileChanged;
            this.watcher.Deleted += OnCollectionFileChanged;
        }
    }

    private void OnCollectionFileChanged(object sender, FileSystemEventArgs e) {
        if (e.FullPath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) {
            return;
        }

        this.isStale = true;
    }

    public void MarkStale() => this.isStale = true;

    public PenumbraCollection? GetCollection(string collectionId) {
        if (string.IsNullOrEmpty(collectionId)) {
            return null;
        }

        return collectionCache.TryGetValue(collectionId, out var collection) ? collection : null;
    }

    public IEnumerable<PenumbraCollection> GetAllCollections() => this.collectionCache.Values;

    public async Task SyncDataAsync() {
        if (isLoading || !isStale) {
            return;
        }

        isLoading = true;

        try {
            await Task.Run(() => {
                var newCache = new Dictionary<string, PenumbraCollection>();
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var collectionsDir = Path.Combine(appData, "XIVLauncher", "pluginConfigs", "Penumbra", "collections");

                if (!Directory.Exists(collectionsDir)) {
                    return;
                }

                var files = Directory.GetFiles(collectionsDir, "*.json");
                foreach (var file in files) {
                    try {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var jsonContent = File.ReadAllText(file);
                        using var doc = JsonDocument.Parse(jsonContent);
                        var root = doc.RootElement;

                        var collection = new PenumbraCollection {
                            Id = fileName,
                            Name = root.TryGetProperty("Name", out var nameProp) ? nameProp.GetString() ?? fileName : fileName
                        };

                        if (root.TryGetProperty("Inheritance", out var inheritanceProp) && inheritanceProp.ValueKind == JsonValueKind.Array) {
                            foreach (var element in inheritanceProp.EnumerateArray()) {
                                var parentId = element.GetString();
                                if (!string.IsNullOrEmpty(parentId)) {
                                    collection.ParentIds.Add(parentId);
                                }
                            }
                        }

                        if (root.TryGetProperty("Settings", out var settingsProp) && settingsProp.ValueKind == JsonValueKind.Object) {
                            foreach (var modProp in settingsProp.EnumerateObject()) {
                                var modId = modProp.Name;
                                var modData = modProp.Value;
                                var mod = new PenumbraMod {
                                    Id = modId,
                                    Name = modId,
                                    SourceCollectionName = collection.Name
                                };

                                if (modData.TryGetProperty("Enabled", out var enabledProp)) {
                                    mod.IsEnabled = enabledProp.GetBoolean();
                                }

                                if (modData.TryGetProperty("Priority", out var priorityProp) && priorityProp.TryGetInt32(out int priority)) {
                                    mod.Priority = priority;
                                }

                                if (modData.TryGetProperty("Settings", out var modOptionsProp) && modOptionsProp.ValueKind == JsonValueKind.Object) {
                                    foreach (var optionGroup in modOptionsProp.EnumerateObject()) {
                                        if (optionGroup.Value.TryGetUInt32(out uint optionValue)) {
                                            mod.Settings[optionGroup.Name] = optionValue;
                                        }
                                    }
                                }

                                collection.LocalSettings[modId] = mod;
                            }
                        }
                        newCache[collection.Id] = collection;
                    } catch { /* Suppress runtime transient disk locks */ }
                }

                collectionCache.Clear();
                foreach (var kvp in newCache) {
                    collectionCache[kvp.Key] = kvp.Value;
                }

                this.isStale = false;
            });
        } finally {
            isLoading = false;
        }
    }

    public EffectiveCollectionState ComputeEffectiveState(string activeCollectionId) {
        var state = new EffectiveCollectionState();
        var visited = new HashSet<string>();
        var lineage = new List<PenumbraCollection>();

        BuildLineage(activeCollectionId, visited, lineage);
        lineage.Reverse();

        // 1. Resolve basic enablement hierarchy
        foreach (var collection in lineage) {
            state.HierarchyNames.Add(collection.Name);
            foreach (var kvp in collection.LocalSettings) {
                var modId = kvp.Key;
                var localModData = kvp.Value;

                state.EffectiveMods[modId] = new PenumbraMod {
                    Id = modId,
                    Name = localModData.Name,
                    IsEnabled = localModData.IsEnabled,
                    Priority = localModData.Priority,
                    SourceCollectionName = collection.Name,
                    Settings = new Dictionary<string, uint>(localModData.Settings, StringComparer.OrdinalIgnoreCase)
                };
            }
        }

        // 2. Compute dynamic file conflicts using scanner cache entries AND active options
        // We now store the ModName alongside ModId to feed the OverwrittenBy property
        var globalFileOwnership = new Dictionary<string, (string ModId, string ModName, int Priority)>(StringComparer.OrdinalIgnoreCase);
        var conflictingMods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var scannerCache = this.modScannerManager.ModCache;

        var evaluatedMods = new List<PenumbraMod>();
        foreach (var mod in state.EffectiveMods.Values) {
            if (mod.IsEnabled) {
                evaluatedMods.Add(mod);
            }
        }

        // Sort mods by priority (highest first)
        evaluatedMods.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        foreach (var mod in evaluatedMods) {
            if (scannerCache.TryGetValue(mod.Id, out var cachedModData)) {

                var activeModPaths = new HashSet<string>(cachedModData.ModifiedGamePaths, StringComparer.OrdinalIgnoreCase);

                foreach (var kvp in cachedModData.OptionGroups) {
                    var groupName = kvp.Key;
                    var groupDef = kvp.Value;
                    uint userSetting = mod.Settings.TryGetValue(groupName, out var val) ? val : 0;

                    if (groupDef.Type.Equals("Multi", StringComparison.OrdinalIgnoreCase)) {
                        for (int i = 0; i < groupDef.OptionPaths.Count; i++) {
                            if ((userSetting & (1u << i)) != 0) {
                                foreach (var path in groupDef.OptionPaths[i]) {
                                    activeModPaths.Add(path);
                                }
                            }
                        }
                    } else {
                        int index = (int)userSetting;
                        if (index >= 0 && index < groupDef.OptionPaths.Count) {
                            foreach (var path in groupDef.OptionPaths[index]) {
                                activeModPaths.Add(path);
                            }
                        }
                    }
                }

                foreach (var gamePath in activeModPaths) {
                    if (globalFileOwnership.TryGetValue(gamePath, out var ownerInfo)) {
                        // FIX: Only the current mod (the loser) is marked as conflicting
                        conflictingMods.Add(mod.Id);

                        // Track which higher-priority mod is crushing this file
                        mod.OverwrittenBy.Add(ownerInfo.ModName);

                        // Analyze the file path to determine the equipment slot
                        mod.ConflictingSlots.Add(ParseEquipmentSlot(gamePath));
                    } else {
                        // Take ownership of the file
                        globalFileOwnership[gamePath] = (mod.Id, mod.Name, mod.Priority);
                    }
                }
            }
        }

        state.ConflictModCount = conflictingMods.Count;

        foreach (var conflictId in conflictingMods) {
            if (state.EffectiveMods.TryGetValue(conflictId, out var conflictingMod)) {
                state.ConflictingMods.Add(conflictingMod);
            }
        }

        state.ConflictingMods.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return state;
    }

    // Helper method to extract the equipment slot from FFXIV game paths
    private string ParseEquipmentSlot(string path) {
        if (string.IsNullOrEmpty(path)) {
            return "Unknown";
        }

        var lowerPath = path.ToLowerInvariant();

        // Weapons
        if (lowerPath.Contains("chara/weapon")) {
            return "Weapon";
        }

        // Common equipment and accessories suffixes
        if (lowerPath.Contains("_met.") || lowerPath.Contains("_met_")) {
            return "Head";
        }

        if (lowerPath.Contains("_top.") || lowerPath.Contains("_top_")) {
            return "Body";
        }

        if (lowerPath.Contains("_glv.") || lowerPath.Contains("_glv_")) {
            return "Hands";
        }

        if (lowerPath.Contains("_dwn.") || lowerPath.Contains("_dwn_")) {
            return "Legs";
        }

        if (lowerPath.Contains("_sho.") || lowerPath.Contains("_sho_")) {
            return "Feet";
        }

        if (lowerPath.Contains("_ear.") || lowerPath.Contains("_ear_")) {
            return "Earrings";
        }

        if (lowerPath.Contains("_nek.") || lowerPath.Contains("_nek_")) {
            return "Necklace";
        }

        if (lowerPath.Contains("_wrs.") || lowerPath.Contains("_wrs_")) {
            return "Bracelets";
        }

        if (lowerPath.Contains("_rir.") || lowerPath.Contains("_rir_") || lowerPath.Contains("_ril.") || lowerPath.Contains("_ril_")) {
            return "Ring";
        }

        // Character customization
        if (lowerPath.Contains("chara/human/")) {
            if (lowerPath.Contains("face")) {
                return "Face";
            }

            if (lowerPath.Contains("hair")) {
                return "Hair";
            }

            if (lowerPath.Contains("tail")) {
                return "Tail";
            }

            return "Body (Base)";
        }

        return "Other";
    }

    private void BuildLineage(string collectionId, HashSet<string> visited, List<PenumbraCollection> lineage) {
        if (!visited.Add(collectionId)) {
            return;
        }

        var collection = GetCollection(collectionId);
        if (collection == null) {
            return;
        }

        lineage.Add(collection);
        foreach (var parentId in collection.ParentIds) {
            BuildLineage(parentId, visited, lineage);
        }
    }

    public void Dispose() {
        if (this.watcher != null) {
            this.watcher.Changed -= OnCollectionFileChanged;
            this.watcher.Created -= OnCollectionFileChanged;
            this.watcher.Deleted -= OnCollectionFileChanged;
            this.watcher.Dispose();
        }
    }
}