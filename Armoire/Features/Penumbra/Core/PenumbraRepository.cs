namespace Armoire.Features.Penumbra.Core;

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
        var globalFileOwnership = new Dictionary<string, (string ModId, int Priority)>(StringComparer.OrdinalIgnoreCase);
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

                // A. Gather ALL effective paths for this mod (Default + Active Options)
                var activeModPaths = new HashSet<string>(cachedModData.ModifiedGamePaths, StringComparer.OrdinalIgnoreCase);

                foreach (var kvp in cachedModData.OptionGroups) {
                    var groupName = kvp.Key;
                    var groupDef = kvp.Value;

                    // If the user hasn't configured this group, default value is 0
                    uint userSetting = mod.Settings.TryGetValue(groupName, out var val) ? val : 0;

                    if (groupDef.Type.Equals("Multi", StringComparison.OrdinalIgnoreCase)) {
                        // MULTI: userSetting is a bitmask
                        for (int i = 0; i < groupDef.OptionPaths.Count; i++) {
                            // If the i-th bit is 1, the option is checked
                            if ((userSetting & (1u << i)) != 0) {
                                foreach (var path in groupDef.OptionPaths[i]) {
                                    activeModPaths.Add(path);
                                }
                            }
                        }
                    } else {
                        // SINGLE: userSetting is the direct index of the chosen option
                        int index = (int)userSetting;
                        if (index >= 0 && index < groupDef.OptionPaths.Count) {
                            foreach (var path in groupDef.OptionPaths[index]) {
                                activeModPaths.Add(path);
                            }
                        }
                    }
                }

                // B. Evaluate conflicts on this final list of paths
                foreach (var gamePath in activeModPaths) {
                    if (globalFileOwnership.TryGetValue(gamePath, out var ownerInfo)) {
                        // Conflict detected! Mark the loser AND the winner.
                        conflictingMods.Add(mod.Id);
                        conflictingMods.Add(ownerInfo.ModId);
                    } else {
                        // Take ownership of the file
                        globalFileOwnership[gamePath] = (mod.Id, mod.Priority);
                    }
                }
            }
        }

        state.ConflictModCount = conflictingMods.Count;

        // Map the string IDs back to their full PenumbraMod objects for the UI
        foreach (var conflictId in conflictingMods) {
            if (state.EffectiveMods.TryGetValue(conflictId, out var conflictingMod)) {
                state.ConflictingMods.Add(conflictingMod);
            }
        }

        // Alphabetical sort for better UX
        state.ConflictingMods.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

        return state;
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