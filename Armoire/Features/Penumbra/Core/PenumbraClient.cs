namespace Armoire.Features.Penumbra.Core;

using Armoire.Features.Penumbra.Interfaces;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using global::Penumbra.Api.IpcSubscribers;
using System;

public class PenumbraClient : IPenumbraClient {
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IPluginLog pluginLog;

    // Les souscriptions utilisent maintenant les types stricts de l'API Penumbra
    private readonly ApiVersion apiVersionSubscriber;
    private readonly GetModList getModListSubscriber;
    private readonly GetCollectionForObject getCollectionForObjectSubscriber;

    public PenumbraClient(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog) {
        this.pluginInterface = pluginInterface;
        this.pluginLog = pluginLog;

        // Initialisation propre via le wrapper officiel
        this.apiVersionSubscriber = new ApiVersion(pluginInterface);
        this.getModListSubscriber = new GetModList(pluginInterface);
        this.getCollectionForObjectSubscriber = new GetCollectionForObject(pluginInterface);
    }

    public bool IsEnabled() {
        try {
            // Le wrapper s'occupe de tout
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
            var mods = this.getModListSubscriber.Invoke();
            return mods?.Count ?? 0;
        } catch {
            return 0;
        }
    }

    public (Guid Id, string Name) GetActiveCollection() {
        if (!IsEnabled()) {
            return (Guid.Empty, string.Empty);
        }

        try {
            // L'index 0 = Joueur local
            var result = this.getCollectionForObjectSubscriber.Invoke(0);

            // On extrait la collection effective du tuple renvoyé par l'API
            var collection = result.EffectiveCollection;

            if (collection.Id == Guid.Empty && string.IsNullOrEmpty(collection.Name)) {
                return (Guid.Empty, string.Empty);
            }

            this.pluginLog.Info($"[PenumbraClient] Wrapper a retourné -> Id: {collection.Id}, Name: '{collection.Name}'");
            return (collection.Id, collection.Name);

        } catch (Exception ex) {
            this.pluginLog.Warning(ex, "[PenumbraClient] L'API Penumbra n'est pas encore prête ou a échoué.");
            return (Guid.Empty, string.Empty);
        }
    }
}