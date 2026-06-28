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

public class PenumbraRepository : IPenumbraRepository {
    private readonly IPluginLog pluginLog;
    private readonly Dictionary<string, PenumbraCollection> collectionCache = new();
    private bool isLoading = false;

    public bool IsLoading => isLoading;

    public PenumbraRepository(IPluginLog pluginLog) {
        this.pluginLog = pluginLog;
    }

    public PenumbraCollection? GetCollection(string collectionId) {
        if (string.IsNullOrEmpty(collectionId)) {
            return null;
        }

        return collectionCache.TryGetValue(collectionId, out var collection) ? collection : null;
    }

    public async Task SyncDataAsync() {
        if (isLoading) {
            return;
        }

        isLoading = true;

        try {
            // Déporte tout le travail lourd (Disque + CPU JSON) sur un thread d'arrière-plan
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
                                    Name = modId, // Penumbra n'enregistre pas le nom humain du mod ici, on garde l'ID de dossier
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
                    } catch (Exception ex) {
                        this.pluginLog.Warning(ex, $"Failed to parse Penumbra collection file: {file}");
                    }
                }

                // Remplacement atomique du cache pour éviter les conflits d'accès avec l'UI
                collectionCache.Clear();
                foreach (var kvp in newCache) {
                    collectionCache[kvp.Key] = kvp.Value;
                }
            });
        } catch (Exception ex) {
            this.pluginLog.Error(ex, "Failed to synchronize Penumbra data.");
        } finally {
            isLoading = false;
        }
    }

    public EffectiveCollectionState ComputeEffectiveState(string activeCollectionId) {
        var state = new EffectiveCollectionState();
        var visited = new HashSet<string>();

        // 1. On récupère d'abord l'ordre exact de l'héritage (du plus lointain au plus proche)
        var lineage = new List<PenumbraCollection>();
        BuildLineage(activeCollectionId, visited, lineage);
        lineage.Reverse(); // On inverse pour traiter les parents d'abord, l'enfant écrasera à la fin

        // 2. On applique les configurations en cascade
        foreach (var collection in lineage) {
            state.HierarchyNames.Add(collection.Name);

            foreach (var kvp in collection.LocalSettings) {
                var modId = kvp.Key;
                var localModData = kvp.Value;

                // L'enfant écrase systématiquement le comportement du parent
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
}