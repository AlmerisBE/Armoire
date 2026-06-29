namespace Armoire.Features.PenumbraIpc;

using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using global::Penumbra.Api.IpcSubscribers;
using System;
using System.Collections.Generic;

public class PenumbraClient : IPenumbraClient, IDisposable {
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IPluginLog pluginLog;

    private readonly ApiVersion apiVersionSubscriber;
    private readonly GetModList getModListSubscriber;
    private readonly GetCollectionForObject getCollectionForObjectSubscriber;
    private readonly GetModDirectory getModDirectorySubscriber;

    private readonly ICallGateSubscriber<Action> initializedSubscriber;
    private readonly ICallGateSubscriber<Action> disposedSubscriber;

    public event Action? OnInitialized;
    public event Action? OnDisposed;

    public PenumbraClient(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog) {
        this.pluginInterface = pluginInterface;
        this.pluginLog = pluginLog;

        this.apiVersionSubscriber = new ApiVersion(pluginInterface);
        this.getModListSubscriber = new GetModList(pluginInterface);
        this.getCollectionForObjectSubscriber = new GetCollectionForObject(pluginInterface);
        this.getModDirectorySubscriber = new GetModDirectory(pluginInterface);

        // Bind Penumbra lifecycle events safely
        this.initializedSubscriber = pluginInterface.GetIpcSubscriber<Action>("Penumbra.Initialized");
        this.initializedSubscriber.Subscribe(HandleInitialized);

        this.disposedSubscriber = pluginInterface.GetIpcSubscriber<Action>("Penumbra.Disposed");
        this.disposedSubscriber.Subscribe(HandleDisposed);
    }

    private void HandleInitialized() {
        this.pluginLog.Info("[PenumbraClient] Penumbra IPC is now available.");
        this.OnInitialized?.Invoke();
    }

    private void HandleDisposed() {
        this.pluginLog.Info("[PenumbraClient] Penumbra IPC was disposed.");
        this.OnDisposed?.Invoke();
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
        return this.GetRawModsList().Count;
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

    public string GetModDirectory() {
        if (!IsEnabled()) {
            return string.Empty;
        }

        try {
            return this.getModDirectorySubscriber.Invoke();
        } catch (Exception ex) {
            this.pluginLog.Warning(ex, "[PenumbraClient] Failed to retrieve mod directory.");
            return string.Empty;
        }
    }

    public Dictionary<string, string> GetRawModsList() {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!IsEnabled()) {
            return result;
        }

        try {
            var ipcList = this.getModListSubscriber.Invoke();
            if (ipcList != null) {
                foreach (var kvp in ipcList) {
                    result[kvp.Key] = kvp.Value;
                }
            } else {
                this.pluginLog.Error("[PenumbraClient] Penumbra IPC returned null for mod list. (Crash or timeout)");
            }
        } catch (Exception ex) {
            this.pluginLog.Error(ex, "[PenumbraClient] Fatal error while processing raw mod list.");
        }

        return result;
    }

    public void Dispose() {
        // Clean up IPC subscriptions to prevent memory leaks
        this.initializedSubscriber.Unsubscribe(HandleInitialized);
        this.disposedSubscriber.Unsubscribe(HandleDisposed);
    }
}