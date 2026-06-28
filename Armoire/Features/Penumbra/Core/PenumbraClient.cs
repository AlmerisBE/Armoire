namespace Armoire.Features.Penumbra.Core;

using Armoire.Features.Penumbra.Interfaces;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public class PenumbraClient : IPenumbraClient {
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IPluginLog pluginLog;

    private readonly ICallGateSubscriber<int> apiVersionSubscriber;
    private readonly ICallGateSubscriber<IDictionary<string, string>> getModListSubscriber;
    private readonly ICallGateSubscriber<int, (string, string)> getCollectionForObjectSubscriber;

    public PenumbraClient(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog) {
        this.pluginInterface = pluginInterface;
        this.pluginLog = pluginLog;

        this.apiVersionSubscriber = pluginInterface.GetIpcSubscriber<int>("Penumbra.ApiVersion");
        this.getModListSubscriber = pluginInterface.GetIpcSubscriber<IDictionary<string, string>>("Penumbra.GetModList");
        this.getCollectionForObjectSubscriber = pluginInterface.GetIpcSubscriber<int, (string, string)>("Penumbra.GetCollectionForObject");
    }

    public bool IsEnabled() {
        try {
            this.apiVersionSubscriber.InvokeFunc();
            return true;
        } catch (Exception) {
            return false;
        }
    }

    public int GetModsCount() {
        if (!IsEnabled()) {
            return 0;
        }

        try {
            var mods = this.getModListSubscriber.InvokeFunc();
            return mods?.Count ?? 0;
        } catch (Exception) {
            return 0;
        }
    }

    public (List<string> Hierarchy, int TotalMods, int EnabledMods) GetActiveCollectionDetails() {
        var hierarchy = new List<string>();
        int totalMods = 0;
        int enabledMods = 0;

        if (!IsEnabled()) {
            return (hierarchy, totalMods, enabledMods);
        }

        try {
            var (element1, element2) = this.getCollectionForObjectSubscriber.InvokeFunc(0);

            string activeGuid = Guid.TryParse(element1, out _) ? element1 : (Guid.TryParse(element2, out _) ? element2 : string.Empty);
            string activeName = activeGuid == element1 ? element2 : element1;

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var collectionsDir = Path.Combine(appData, "XIVLauncher", "pluginConfigs", "Penumbra", "collections");

            if (Directory.Exists(collectionsDir) && !string.IsNullOrEmpty(activeGuid)) {
                var visited = new HashSet<string>();
                var filePath = Path.Combine(collectionsDir, $"{activeGuid}.json");

                // Lecture de la collection principale pour extraire les statistiques de ses mods
                if (File.Exists(filePath)) {
                    try {
                        var jsonContent = File.ReadAllText(filePath);
                        using var doc = JsonDocument.Parse(jsonContent);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("Settings", out var settingsProp) && settingsProp.ValueKind == JsonValueKind.Object) {
                            foreach (var modProp in settingsProp.EnumerateObject()) {
                                totalMods++;
                                if (modProp.Value.TryGetProperty("Enabled", out var enabledProp) && enabledProp.GetBoolean()) {
                                    enabledMods++;
                                }
                            }
                        }
                    } catch {
                        // Ignoré si le fichier est temporairement verrouillé
                    }
                }

                // Résolution de la hiérarchie d'héritage
                ResolveInheritance(activeGuid, collectionsDir, visited, hierarchy);
            }

            if (hierarchy.Count == 0 && !string.IsNullOrEmpty(activeName)) {
                hierarchy.Add(activeName);
            }
        } catch (Dalamud.Plugin.Ipc.Exceptions.IpcNotReadyError) {
            // Cycle de chargement attendu
        } catch (Exception ex) {
            this.pluginLog.Warning(ex, "Failed to retrieve collection details.");
        }

        return (hierarchy, totalMods, enabledMods);
    }

    private void ResolveInheritance(string collectionId, string collectionsDir, HashSet<string> visited, List<string> hierarchy) {
        if (string.IsNullOrEmpty(collectionId) || !visited.Add(collectionId)) {
            return;
        }

        var filePath = Path.Combine(collectionsDir, $"{collectionId}.json");
        if (!File.Exists(filePath)) {
            return;
        }

        try {
            var jsonContent = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            if (root.TryGetProperty("Name", out var nameProp)) {
                hierarchy.Add(nameProp.GetString() ?? "Unknown");
            }

            if (root.TryGetProperty("Inheritance", out var inheritanceProp) && inheritanceProp.ValueKind == JsonValueKind.Array) {
                foreach (var element in inheritanceProp.EnumerateArray()) {
                    ResolveInheritance(element.GetString()!, collectionsDir, visited, hierarchy);
                }
            }
        } catch {
            // Ignoré si fichier corrompu
        }
    }
}