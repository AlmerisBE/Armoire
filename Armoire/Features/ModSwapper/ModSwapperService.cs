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
        string rootDir = this.penumbraClient.GetModDirectory();
        if (string.IsNullOrEmpty(rootDir)) {
            this.pluginLog.Error("[ModSwapper] Cannot retrieve Penumbra root mod directory.");
            return false;
        }

        string fullModPath = Path.Combine(rootDir, modId);
        if (!Directory.Exists(fullModPath)) {
            this.pluginLog.Error($"[ModSwapper] Mod directory not found at {fullModPath}");
            return false;
        }

        try {
            this.pluginLog.Info($"[ModSwapper] Starting swap for {modId} ({slotKey} -> {targetModelId})");

            // Get all json files (default_mod.json AND group_*.json)
            var jsonFiles = Directory.GetFiles(fullModPath, "*.json", SearchOption.TopDirectoryOnly);
            bool anyFilesChanged = false;

            foreach (var configFile in jsonFiles) {
                string fileName = Path.GetFileName(configFile);
                // Ignore metadata
                if (fileName.Equals("meta.json", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                string backupPath = configFile + ".armoire_bak";
                string jsonContent = File.ReadAllText(configFile);
                var root = JToken.Parse(jsonContent);

                // Recursively search and replace paths in this file
                if (RecurseAndReplace(root, slotKey, targetModelId)) {
                    // Create backup only if changes were made and no backup exists yet
                    if (!File.Exists(backupPath)) {
                        File.Copy(configFile, backupPath);
                        this.pluginLog.Info($"[ModSwapper] Created safety backup: {fileName}.armoire_bak");
                    }

                    File.WriteAllText(configFile, root.ToString());
                    anyFilesChanged = true;
                }
            }

            if (anyFilesChanged) {
                this.pluginLog.Info("[ModSwapper] Successfully saved modified JSON files.");
                this.penumbraClient.ReloadMod(modId);
                this.penumbraClient.RedrawAll();
                return true;
            }

            this.pluginLog.Info("[ModSwapper] No matching paths found to swap for this specific slot in any JSON file.");
            return false;

        } catch (Exception ex) {
            this.pluginLog.Error(ex, $"[ModSwapper] Failed to perform swap for mod {modId}");
            return false;
        }
    }

    public bool ResetMod(string modId) {
        string rootDir = this.penumbraClient.GetModDirectory();
        if (string.IsNullOrEmpty(rootDir)) {
            return false;
        }

        string fullModPath = Path.Combine(rootDir, modId);
        var backupFiles = Directory.GetFiles(fullModPath, "*.armoire_bak", SearchOption.TopDirectoryOnly);

        if (backupFiles.Length == 0) {
            this.pluginLog.Warning($"[ModSwapper] No backups found to restore for {modId}. Mod is already in its original state.");
            return false;
        }

        try {
            // Restore all found backups over their modified original files
            foreach (var backupPath in backupFiles) {
                string originalFilePath = backupPath.Replace(".armoire_bak", "");
                File.Copy(backupPath, originalFilePath, overwrite: true);
                File.Delete(backupPath);
            }

            this.penumbraClient.ReloadMod(modId);
            this.penumbraClient.RedrawAll();

            this.pluginLog.Info($"[ModSwapper] Successfully restored {backupFiles.Length} backups for {modId}");
            return true;
        } catch (Exception ex) {
            this.pluginLog.Error(ex, $"[ModSwapper] Failed to restore backup for mod {modId}");
            return false;
        }
    }

    /// <summary>
    /// Recursively traverses a JSON token to find "Files" and "FileSwaps" objects and applies replacements.
    /// </summary>
    private bool RecurseAndReplace(JToken token, string slotKey, string targetModelId) {
        bool changed = false;

        if (token is JObject obj) {
            // 1. Process known target dictionaries if they exist
            if (obj["Files"] is JObject files) {
                changed |= ProcessJObjectPaths(files, slotKey, targetModelId);
            }
            if (obj["FileSwaps"] is JObject fileSwaps) {
                changed |= ProcessJObjectPaths(fileSwaps, slotKey, targetModelId);
            }

            // 2. Continue traversing down the tree (e.g., inside an "Options" array)
            foreach (var prop in obj.Properties()) {
                // Skip traversing into the dictionaries we literally just processed
                if (prop.Name.Equals("Files", StringComparison.OrdinalIgnoreCase) ||
                    prop.Name.Equals("FileSwaps", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                changed |= RecurseAndReplace(prop.Value, slotKey, targetModelId);
            }
        } else if (token is JArray arr) {
            foreach (var item in arr) {
                changed |= RecurseAndReplace(item, slotKey, targetModelId);
            }
        }

        return changed;
    }

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

    private bool IsPathMatchingSlot(string path, string slotKey) {
        string lowerPath = path.ToLowerInvariant();

        if (slotKey != "wpn" && slotKey != "sub" && slotKey != "custom" && slotKey != "unknown") {
            return lowerPath.Contains($"_{slotKey}.") || lowerPath.Contains($"_{slotKey}_");
        }

        if (slotKey == "wpn" || slotKey == "sub") {
            return lowerPath.Contains("chara/weapon/");
        }

        return false;
    }
}