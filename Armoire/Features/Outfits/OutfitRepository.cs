namespace Armoire.Features.Outfits;

using Armoire.Features.Outfits.Models;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public class OutfitRepository {
    private readonly string filePath;
    private readonly IPluginLog pluginLog;
    private List<ArmoireOutfit> outfits = new();

    public IReadOnlyList<ArmoireOutfit> Outfits => this.outfits;

    public OutfitRepository(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog) {
        this.pluginLog = pluginLog;
        // Stores the file safely in %AppData%\XIVLauncher\pluginConfigs\Armoire\outfits.json
        this.filePath = Path.Combine(pluginInterface.ConfigDirectory.FullName, "outfits.json");
        Load();
    }

    private void Load() {
        if (!File.Exists(this.filePath)) {
            return;
        }

        try {
            var json = File.ReadAllText(this.filePath);
            this.outfits = JsonSerializer.Deserialize<List<ArmoireOutfit>>(json) ?? new();
        } catch (Exception ex) {
            this.pluginLog.Error(ex, "[OutfitRepository] Failed to load outfits.json. Initializing empty list.");
            this.outfits = new();
        }
    }

    private void Save() {
        try {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this.outfits, options);
            File.WriteAllText(this.filePath, json);
        } catch (Exception ex) {
            this.pluginLog.Error(ex, "[OutfitRepository] Failed to save outfits.json.");
        }
    }

    public void AddOutfit(ArmoireOutfit outfit) {
        this.outfits.Add(outfit);
        Save();
    }

    public void DeleteOutfit(Guid id) {
        this.outfits.RemoveAll(o => o.Id == id);
        Save();
    }

    public void UpdateOutfit(ArmoireOutfit outfit) {
        Save();
    }
}