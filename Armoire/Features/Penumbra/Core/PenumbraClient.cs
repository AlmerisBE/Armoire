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

        // Retour à la route IPC robuste basée sur le nom du personnage !
        this.getCollectionForCharacterSubscriber = pluginInterface.GetIpcSubscriber<string, (Guid, string)>("Penumbra.GetCollectionForCharacter");
    }

    public bool IsEnabled() {
        try {
            this.apiVersionSubscriber.InvokeFunc();
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
            return this.getModListSubscriber.InvokeFunc()?.Count ?? 0;
        } catch {
            return 0;
        }
    }

    public (Guid Id, string Name) GetActiveCollection(string characterName) {
        if (!IsEnabled() || string.IsNullOrEmpty(characterName)) {
            return (Guid.Empty, string.Empty);
        }

        try {
            return this.getCollectionForCharacterSubscriber.InvokeFunc(characterName);
        } catch (Dalamud.Plugin.Ipc.Exceptions.IpcNotReadyError) {
            return (Guid.Empty, string.Empty);
        } catch (Exception ex) {
            this.pluginLog.Warning(ex, "Failed to retrieve active collection from Penumbra IPC.");
            return (Guid.Empty, string.Empty);
        }
    }
}