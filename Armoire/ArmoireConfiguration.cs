namespace Armoire;

using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

[Serializable]
public class ModifiedModEntry {
    public string ModName { get; set; } = string.Empty;

    // Maps a SlotKey (e.g., "top") to the TargetModelId (e.g., "e0521")
    public Dictionary<string, string> Swaps { get; set; } = new();
}

[Serializable]
public class ArmoireConfiguration : IPluginConfiguration {
    public int Version { get; set; } = 1;

    // The core memory of Armoire: ModId -> Swap Data
    public Dictionary<string, ModifiedModEntry> ModifiedMods { get; set; } = new();

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface) {
        this.pluginInterface = pluginInterface;
    }

    public void Save() {
        this.pluginInterface?.SavePluginConfig(this);
    }
}