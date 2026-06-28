namespace Armoire.Features.Penumbra.Core;

using Armoire.Features.Penumbra.Interfaces;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;

public class PenumbraClient : IPenumbraClient {
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IPluginLog pluginLog;

    private readonly ICallGateSubscriber<int> apiVersionSubscriber;
    private readonly ICallGateSubscriber<IDictionary<string, string>> getModListSubscriber;
    private readonly ICallGateSubscriber<string, (Guid, string)> getCollectionForCharacterSubscriber;

    public PenumbraClient(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog) {
        this.pluginInterface = pluginInterface;
        this.pluginLog = pluginLog;

        this.apiVersionSubscriber = pluginInterface.GetIpcSubscriber<int>("Penumbra.ApiVersion");
        this.getModListSubscriber = pluginInterface.GetIpcSubscriber<IDictionary<string, string>>("Penumbra.GetModList");

        // Nouvelle route IPC pour lire la collection d'un personnage
        this.getCollectionForCharacterSubscriber = pluginInterface.GetIpcSubscriber<string, (Guid, string)>("Penumbra.GetCollectionForCharacter");
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
        } catch (Exception ex) {
            this.pluginLog.Error(ex, "Failed to retrieve the mod list from Penumbra.");
            return 0;
        }
    }

    public string? GetCollectionForCharacter(string characterName) {
        if (!IsEnabled()) {
            return null;
        }

        try {
            // L'IPC renvoie un Tuple (Guid ID, string Nom), on extrait le Nom (Item2)
            var result = this.getCollectionForCharacterSubscriber.InvokeFunc(characterName);
            return result.Item2;
        } catch (Exception ex) {
            this.pluginLog.Warning(ex, "Failed to retrieve the collection for character from Penumbra.");
            return null;
        }
    }
}