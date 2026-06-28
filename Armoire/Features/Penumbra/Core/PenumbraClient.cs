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
    private readonly ICallGateSubscriber<int, (Guid, string)> getCollectionForObjectSubscriber;

    public PenumbraClient(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog) {
        this.pluginInterface = pluginInterface;
        this.pluginLog = pluginLog;

        this.apiVersionSubscriber = pluginInterface.GetIpcSubscriber<int>("Penumbra.ApiVersion");
        this.getModListSubscriber = pluginInterface.GetIpcSubscriber<IDictionary<string, string>>("Penumbra.GetModList");

        this.getCollectionForObjectSubscriber = pluginInterface.GetIpcSubscriber<int, (Guid, string)>("Penumbra.GetCollectionForObject");
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
            // L'index 0 correspond au joueur local
            var (activeId, activeName) = this.getCollectionForObjectSubscriber.InvokeFunc(0);

            // On remonte d'un dossier pour trouver les configurations de Penumbra
            var penumbraDir = Path.Combine(this.pluginInterface.ConfigDirectory.Parent!.FullName, "Penumbra");
            var collectionsDir = Path.Combine(penumbraDir, "collections");

            if (Directory.Exists(collectionsDir)) {
                var visited = new HashSet<string>();
                ResolveInheritance(activeId.ToString(), collectionsDir, visited, hierarchy);
            } else {
                hierarchy.Add(activeName); // Fallback de sécurité
            }
        } catch (Dalamud.Plugin.Ipc.Exceptions.IpcNotReadyError) {
            // L'IPC de Penumbra n'est pas encore prêt, on ignore silencieusement pour éviter le spam de log.
            // Le Tick() de l'interface réessayera automatiquement 2 secondes plus tard !
        } catch (Exception ex) {
            this.pluginLog.Warning(ex, "Failed to retrieve collection hierarchy from Penumbra.");
        }

        return hierarchy;
    }

    private void ResolveInheritance(string collectionId, string collectionsDir, HashSet<string> visited, List<string> hierarchy) {
        if (!visited.Add(collectionId)) {
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
            // Si un fichier est corrompu, on l'ignore silencieusement
        }
    }
}