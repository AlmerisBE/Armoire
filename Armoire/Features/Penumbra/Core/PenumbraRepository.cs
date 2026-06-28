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
    private readonly Dictionary<string, PenumbraCollection> collectionCache = new();
    private readonly FileSystemWatcher? watcher;

    private bool isLoading = false;
    private bool isStale = true; // Vrai au démarrage pour forcer la première lecture

    public bool IsLoading => isLoading;
    public bool IsStale => isStale;

    public PenumbraRepository(IPluginLog pluginLog) {
        this.pluginLog = pluginLog;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var collectionsDir = Path.Combine(appData, "XIVLauncher", "pluginConfigs", "Penumbra", "collections");

        if (Directory.Exists(collectionsDir)) {
            // Configuration du watcher pour écouter les sauvegardes de Penumbra
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
        // Ignorer les fichiers temporaires créés par Penumbra lors des sauvegardes
        if (e.FullPath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) {
            return;
        }

        this.pluginLog.Debug($"[PenumbraRepository] Fichier modifié détecté par le système : {e.Name}");
        this.isStale = true;
    }

    public void MarkStale() {
        this.isStale = true;
    }

    public PenumbraCollection? GetCollection(string collectionId) {
        if (string.IsNullOrEmpty(collectionId)) {
            return null;
        }

        return collectionCache.TryGetValue(collectionId, out var collection) ? collection : null;
    }

    public IEnumerable<PenumbraCollection> GetAllCollections() {
        return this.collectionCache.Values;
    }

    public async Task SyncDataAsync() {
        if (isLoading || !isStale) {
            return;
        }

        isLoading = true;

        try {
            await Task.Run(() => {
                this.pluginLog.Info("[PenumbraRepository] Démarrage de la synchronisation asynchrone des collections sur le disque.");
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

                                collection.LocalSettings[modId] = mod;
                            }
                        }

                        newCache[collection.Id] = collection;
                    } catch (Exception) { /* Ignoré silencieusement pour ne pas spammer si le fichier est verrouillé une fraction de seconde */ }
                }

                collectionCache.Clear();
                foreach (var kvp in newCache) {
                    collectionCache[kvp.Key] = kvp.Value;
                }

                // Le cache est désormais à jour
                this.isStale = false;
                this.pluginLog.Info($"[PenumbraRepository] Synchronisation terminée. {collectionCache.Count} collections chargées en mémoire.");
            });
        } catch (Exception ex) {
            this.pluginLog.Error(ex, "[PenumbraRepository] Erreur fatale lors de la synchronisation.");
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
                    SourceCollectionName = collection.Name
                };
            }
        }

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