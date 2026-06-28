namespace Armoire.Features.Penumbra.Core;

using Armoire.Features.Penumbra.Interfaces;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using global::Penumbra.Api.IpcSubscribers;
using System;

public class PenumbraClient : IPenumbraClient {
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IPluginLog pluginLog;

    private readonly ApiVersion apiVersionSubscriber;
    private readonly GetModList getModListSubscriber;
    private readonly GetCollectionForObject getCollectionForObjectSubscriber;

    // NOUVEAU : Le souscripteur officiel pour le dossier des mods
    private readonly GetModDirectory getModDirectorySubscriber;

    public PenumbraClient(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog) {
        this.pluginInterface = pluginInterface;
        this.pluginLog = pluginLog;

        this.apiVersionSubscriber = new ApiVersion(pluginInterface);
        this.getModListSubscriber = new GetModList(pluginInterface);
        this.getCollectionForObjectSubscriber = new GetCollectionForObject(pluginInterface);

        // Initialisation de la route IPC
        this.getModDirectorySubscriber = new GetModDirectory(pluginInterface);
    }

    public bool IsEnabled() {
        try {
            this.apiVersionSubscriber.Invoke();
            return true;
        } catch {
            return false;
        }
    }

    public int GetModsCount() {
        if (!IsEnabled()) {
            return 0;
        }

        try {
            return this.getModListSubscriber.Invoke()?.Count ?? 0;
        } catch {
            return 0;
        }
    }

    public (Guid Id, string Name) GetActiveCollection() {
        if (!IsEnabled()) {
            return (Guid.Empty, string.Empty);
        }

        try {
            var result = this.getCollectionForObjectSubscriber.Invoke(0);
            var collection = result.EffectiveCollection;

            if (collection.Id == Guid.Empty && string.IsNullOrEmpty(collection.Name)) {
                return (Guid.Empty, string.Empty);
            }
            return (collection.Id, collection.Name);
        } catch (Exception) {
            return (Guid.Empty, string.Empty);
        }
    }

    // NOUVEAU : Implémentation de la méthode
    public string GetModDirectory() {
        if (!IsEnabled()) {
            return string.Empty;
        }

        try {
            return this.getModDirectorySubscriber.Invoke();
        } catch (Exception ex) {
            this.pluginLog.Warning(ex, "[PenumbraClient] Impossible de récupérer le dossier des mods.");
            return string.Empty;
        }
    }
}