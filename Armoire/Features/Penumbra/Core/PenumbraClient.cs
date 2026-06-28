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

    public (Guid Id, string Name) GetActiveCollection() {
        if (!IsEnabled()) {
            return (Guid.Empty, string.Empty);
        }

        try {
            return this.getCollectionForObjectSubscriber.InvokeFunc(0);
        } catch (Dalamud.Plugin.Ipc.Exceptions.IpcNotReadyError) {
            return (Guid.Empty, string.Empty);
        } catch (Exception ex) {
            this.pluginLog.Warning(ex, "Failed to retrieve active collection from Penumbra IPC.");
            return (Guid.Empty, string.Empty);
        }
    }
}