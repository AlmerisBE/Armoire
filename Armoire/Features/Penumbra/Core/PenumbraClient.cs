using Armoire.Features.Penumbra.Interfaces;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;

namespace Armoire.Features.Penumbra.Core;

public class PenumbraClient : IPenumbraClient {
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IPluginLog pluginLog;

    private readonly ICallGateSubscriber<int> apiVersionSubscriber;
    private readonly ICallGateSubscriber<IDictionary<string, string>> getModListSubscriber;

    public PenumbraClient(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog) {
        this.pluginInterface = pluginInterface;
        this.pluginLog = pluginLog;

        apiVersionSubscriber = pluginInterface.GetIpcSubscriber<int>("Penumbra.ApiVersion");

        getModListSubscriber = pluginInterface.GetIpcSubscriber<IDictionary<string, string>>("Penumbra.GetModList");
    }

    public bool IsEnabled() {
        try {
            apiVersionSubscriber.InvokeFunc();
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
            var mods = getModListSubscriber.InvokeFunc();
            var count = mods?.Count ?? 0;

            pluginLog.Debug($"Successfully retrieved mod list. Total mods: {count}");

            return count;
        } catch (Exception ex) {
            pluginLog.Error(ex, "Failed to retrieve the mod list from Penumbra.");
            return 0;
        }
    }
}
