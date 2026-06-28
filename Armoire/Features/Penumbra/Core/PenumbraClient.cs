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

    public List<string> GetActiveCollectionHierarchy() {
        var hierarchy = new List<string>();
        if (!IsEnabled()) {
            return hierarchy;
        }

        try {
            // Récupère les chaînes brutes de l'IPC
            var (element1, element2) = this.getCollectionForObjectSubscriber.InvokeFunc(0);

            // Identification dynamique du GUID et du Nom
            string activeGuid = string.Empty;
            string activeName = string.Empty;

            if (Guid.TryParse(element1, out _)) {
                activeGuid = element1;
                activeName = element2;
            } else if (Guid.TryParse(element2, out _)) {
                activeGuid = element2;
                activeName = element1;
            } else {
                activeName = element1;
                activeGuid = element2;
            }

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var collectionsDir = Path.Combine(appData, "XIVLauncher", "pluginConfigs", "Penumbra", "collections");

            if (Directory.Exists(collectionsDir) && !string.IsNullOrEmpty(activeGuid)) {
                var visited = new HashSet<string>();
                ResolveInheritance(activeGuid, collectionsDir, visited, hierarchy);
            }

            // Sécurité : si la lecture du disque échoue, on affiche au moins le nom de la collection principale Reçu par l'IPC
            if (hierarchy.Count == 0 && !string.IsNullOrEmpty(activeName)) {
                hierarchy.Add(activeName);
            }
        } catch (Dalamud.Plugin.Ipc.Exceptions.IpcNotReadyError) {
            // Ignoré proprement pendant le chargement initial
        } catch (Exception ex) {
            this.pluginLog.Warning(ex, "Failed to retrieve collection hierarchy from Penumbra.");
        }

        return hierarchy;
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

            if (root.TryGetProperty("Inheritances", out var inheritancesProp) && inheritancesProp.ValueKind == JsonValueKind.Array) {
                foreach (var element in inheritancesProp.EnumerateArray()) {
                    ResolveInheritance(element.GetString()!, collectionsDir, visited, hierarchy);
                }
            }
        } catch {
            // Ignoré si le fichier est corrompu
        }
    }
}