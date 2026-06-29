namespace Armoire.Features.ModSwapper;

using Armoire.Features.LocalScanner;
using Armoire.Features.PenumbraIpc;
using Dalamud.Plugin.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class ModSwapperService : IModSwapperService {
    private readonly IModScannerManager scannerManager;
    private readonly IPluginLog pluginLog;
    private readonly IPenumbraClient penumbraClient;

    // Regex to strictly match FFXIV equipment/weapon models (e.g., e0123, w1234)
    private readonly Regex modelIdRegex = new Regex(@"([ew]\d{4})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ModSwapperService(IModScannerManager scannerManager, IPluginLog pluginLog, IPenumbraClient penumbraClient) {
        this.scannerManager = scannerManager;
        this.pluginLog = pluginLog;
        this.penumbraClient = penumbraClient;
    }

    public bool PerformSwap(string modId, string slotKey, string targetModelId) {
        // modId represents the physical directory path
        string configPath = Path.Combine(modId, "default_mod.json");
        string backupPath = Path.Combine(modId, "default_mod.armoire_bak");

        if (!File.Exists(configPath)) {
            this.pluginLog.Error($"[ModSwapper] Configuration file not found at {configPath}");
            return false;
        }

        try {
            this.pluginLog.Info($"[ModSwapper] Starting swap for {modId} ({slotKey} -> {targetModelId})");

            // 1. Create a pristine backup if it's the very first time we modify this mod
            if (!File.Exists(backupPath)) {
                File.Copy(configPath, backupPath);
                this.pluginLog.Info("[ModSwapper] Created safety backup: default_mod.armoire_bak");
            }

            // 2. Load and Parse JSON
            string jsonContent = File.ReadAllText(configPath);
            var root = JObject.Parse(jsonContent);
            bool filesChanged = false;

            // 3. Process the "Files" and "FileSwaps" root objects
            if (root["Files"] is JObject files) {
                filesChanged |= ProcessJObjectPaths(files, slotKey, targetModelId);
            }
            if (root["FileSwaps"] is JObject fileSwaps) {
                filesChanged |= ProcessJObjectPaths(fileSwaps, slotKey, targetModelId);
            }

            // 4. Save to disk if any redirection was applied
            if (filesChanged) {
                File.WriteAllText(configPath, root.ToString());
                this.pluginLog.Info("[ModSwapper] Successfully saved modified JSON.");

                this.penumbraClient.ReloadMod(modId);
                this.penumbraClient.RedrawAll();
                return true;
            }

            this.pluginLog.Info("[ModSwapper] No matching paths found to swap for this specific slot.");
            return false;

        } catch (Exception ex) {
            this.pluginLog.Error(ex, $"[ModSwapper] Failed to perform swap for mod {modId}");
            return false;
        }
    }

    public bool ResetMod(string modId) {
        string configPath = Path.Combine(modId, "default_mod.json");
        string backupPath = Path.Combine(modId, "default_mod.armoire_bak");

        if (!File.Exists(backupPath)) {
            this.pluginLog.Warning($"[ModSwapper] No backup found to restore for {modId}. Mod is already in its original state.");
            return false;
        }

        try {
            // Restore the backup over the modified file, then delete the backup so we know it's clean
            File.Copy(backupPath, configPath, overwrite: true);
            File.Delete(backupPath);

            this.penumbraClient.ReloadMod(modId);
            this.penumbraClient.RedrawAll();

            this.pluginLog.Info($"[ModSwapper] Successfully restored backup for {modId}");
            return true;
        } catch (Exception ex) {
            this.pluginLog.Error(ex, $"[ModSwapper] Failed to restore backup for mod {modId}");
            return false;
        }
    }

    /// <summary>
    /// Scans a JSON object, finds keys matching the target slot, and replaces their model IDs with the new one.
    /// </summary>
    private bool ProcessJObjectPaths(JObject dict, string slotKey, string targetModelId) {
        var propertiesToReplace = new List<JProperty>();
        bool hasChanged = false;

        // First pass: identify what needs to change (preventing collection modification exceptions)
        foreach (var prop in dict.Properties()) {
            string originalGamePath = prop.Name;

            if (IsPathMatchingSlot(originalGamePath, slotKey)) {
                string newGamePath = this.modelIdRegex.Replace(originalGamePath, targetModelId);

                if (newGamePath != originalGamePath) {
                    propertiesToReplace.Add(prop);
                }
            }
        }

        // Second pass: apply the changes
        foreach (var prop in propertiesToReplace) {
            string originalGamePath = prop.Name;
            JToken? localFilePath = prop.Value;

            string newGamePath = this.modelIdRegex.Replace(originalGamePath, targetModelId);

            prop.Remove();
            dict[newGamePath] = localFilePath; // Map the new FFXIV vanilla path to the existing modded file
            hasChanged = true;
        }

        return hasChanged;
    }

    /// <summary>
    /// Checks if a raw FFXIV game path corresponds to the currently targeted equipment slot.
    /// </summary>
    private bool IsPathMatchingSlot(string path, string slotKey) {
        string lowerPath = path.ToLowerInvariant();

        // Standard FFXIV equipment suffixes
        if (slotKey != "wpn" && slotKey != "sub" && slotKey != "custom" && slotKey != "unknown") {
            return lowerPath.Contains($"_{slotKey}.") || lowerPath.Contains($"_{slotKey}_");
        }

        // Weapons use a different structure entirely (chara/weapon/...)
        if (slotKey == "wpn" || slotKey == "sub") {
            return lowerPath.Contains("chara/weapon/");
        }

        return false;
    }
}