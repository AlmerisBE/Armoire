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
    private readonly ArmoireConfiguration configuration;

    private readonly Regex modelIdRegex = new Regex(@"([ew]\d{4})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ModSwapperService(IModScannerManager scannerManager, IPluginLog pluginLog, IPenumbraClient penumbraClient, ArmoireConfiguration configuration) {
        this.scannerManager = scannerManager;
        this.pluginLog = pluginLog;
        this.penumbraClient = penumbraClient;
        this.configuration = configuration;
    }

    public bool PerformSwap(string modId, string slotKey, string targetModelId, string textureProviderModId = "") {
        // 1. Process the primary model mod swap
        bool mainSwap = ExecuteSingleSwap(modId, slotKey, targetModelId);
        bool texSwap = false;

        // 2. Process the secondary texture provider mod swap if specified
        if (!string.IsNullOrWhiteSpace(textureProviderModId)) {
            this.pluginLog.Info($"[ModSwapper] Executing secondary texture inheritance swap for {textureProviderModId}");
            texSwap = ExecuteSingleSwap(textureProviderModId, slotKey, targetModelId);
        }

        // 3. Trigger a single global redraw at the end to prevent performance stutter
        if (mainSwap || texSwap) {
            if (mainSwap && this.configuration.ModifiedMods.TryGetValue(modId, out var modEntry)) {
                if (!string.IsNullOrWhiteSpace(textureProviderModId)) {
                    modEntry.TextureProviders[slotKey] = textureProviderModId;
                } else {
                    modEntry.TextureProviders.Remove(slotKey);
                }
                this.configuration.Save();
            }

            this.penumbraClient.RedrawAll();
            return true;
        }

        return false;
    }

    private bool ExecuteSingleSwap(string targetModId, string slotKey, string targetModelId) {
        string rootDir = this.penumbraClient.GetModDirectory();
        if (string.IsNullOrEmpty(rootDir)) {
            return false;
        }

        string fullModPath = Path.Combine(rootDir, targetModId);
        if (!Directory.Exists(fullModPath)) {
            return false;
        }

        try {
            this.pluginLog.Info($"[ModSwapper] Patching {targetModId} ({slotKey} -> {targetModelId})");
            var jsonFiles = Directory.GetFiles(fullModPath, "*.json", SearchOption.TopDirectoryOnly);
            bool anyFilesChanged = false;

            foreach (var configFile in jsonFiles) {
                string fileName = Path.GetFileName(configFile);
                if (fileName.Equals("meta.json", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                string backupPath = configFile + ".armoire_bak";
                string jsonContent = File.ReadAllText(configFile);
                var root = JToken.Parse(jsonContent);

                if (RecurseAndReplace(root, slotKey, targetModelId, fullModPath)) {
                    if (!File.Exists(backupPath)) {
                        File.Copy(configFile, backupPath);
                    }

                    File.WriteAllText(configFile, root.ToString());
                    anyFilesChanged = true;
                }
            }

            if (anyFilesChanged) {
                // Record the modification into the configuration memory persistence layer
                if (!this.configuration.ModifiedMods.TryGetValue(targetModId, out var modEntry)) {
                    // Try to fetch the real mod name from the scanner cache, fallback to directory name
                    string modName = this.scannerManager.ModCache.TryGetValue(targetModId, out var cache)
                        ? cache.ModName
                        : targetModId;

                    modEntry = new ModifiedModEntry { ModName = modName };
                    this.configuration.ModifiedMods[targetModId] = modEntry;
                }

                // Track the swap configuration for this slot
                modEntry.Swaps[slotKey] = targetModelId;
                this.configuration.Save();

                this.penumbraClient.ReloadMod(targetModId);
                return true;
            }

            return false;
        } catch (Exception ex) {
            this.pluginLog.Error(ex, $"[ModSwapper] Failed to perform swap for mod {targetModId}");
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
            // Ensure data cleanup even if backup files are missing to avoid orphaned config rows
            if (this.configuration.ModifiedMods.Remove(modId)) {
                this.configuration.Save();
            }
            return false;
        }

        try {
            // 1. Restore original configuration files from backups
            foreach (var backupPath in backupFiles) {
                string originalFilePath = backupPath.Replace(".armoire_bak", "");
                File.Copy(backupPath, originalFilePath, overwrite: true);
                File.Delete(backupPath);
            }

            // 2. Clean up all generated patched binary models and materials
            var patchedFiles = Directory.GetFiles(fullModPath, "*_armoire.*", SearchOption.AllDirectories);
            foreach (var file in patchedFiles) {
                if (file.EndsWith(".mdl", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".mtrl", StringComparison.OrdinalIgnoreCase)) {
                    File.Delete(file);
                }
            }

            // Remove the mod entry from configuration memory persistence
            if (this.configuration.ModifiedMods.Remove(modId)) {
                this.configuration.Save();
            }

            this.penumbraClient.ReloadMod(modId);
            this.penumbraClient.RedrawAll();
            return true;
        } catch (Exception ex) {
            this.pluginLog.Error(ex, $"[ModSwapper] Failed to restore backup for mod {modId}");
            return false;
        }
    }

    private bool RecurseAndReplace(JToken token, string slotKey, string targetModelId, string fullModPath) {
        bool changed = false;
        if (token is JObject obj) {
            if (obj["Files"] is JObject files) {
                changed |= ProcessJObjectPaths(files, slotKey, targetModelId, fullModPath);
            }
            if (obj["FileSwaps"] is JObject fileSwaps) {
                changed |= ProcessJObjectPaths(fileSwaps, slotKey, targetModelId, fullModPath);
            }

            foreach (var prop in obj.Properties()) {
                if (prop.Name.Equals("Files", StringComparison.OrdinalIgnoreCase) ||
                    prop.Name.Equals("FileSwaps", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                changed |= RecurseAndReplace(prop.Value, slotKey, targetModelId, fullModPath);
            }
        } else if (token is JArray arr) {
            foreach (var item in arr) {
                changed |= RecurseAndReplace(item, slotKey, targetModelId, fullModPath);
            }
        }

        return changed;
    }

    private bool ProcessJObjectPaths(JObject dict, string slotKey, string targetModelId, string fullModPath) {
        var propertiesToReplace = new List<JProperty>();
        bool hasChanged = false;

        foreach (var prop in dict.Properties()) {
            string originalGamePath = prop.Name;
            if (IsPathMatchingSlot(originalGamePath, slotKey)) {
                string newGamePath = this.modelIdRegex.Replace(originalGamePath, targetModelId);
                if (newGamePath != originalGamePath) {
                    propertiesToReplace.Add(prop);
                }
            }
        }

        foreach (var prop in propertiesToReplace) {
            string originalGamePath = prop.Name;
            JToken? localFilePath = prop.Value;

            var match = this.modelIdRegex.Match(originalGamePath);
            string oldModelId = match.Success ? match.Value : string.Empty;
            string newGamePath = this.modelIdRegex.Replace(originalGamePath, targetModelId);
            prop.Remove();

            // --- DUAL BINARY PATCHING ---
            if (localFilePath != null && localFilePath.Type == JTokenType.String) {
                string localStr = localFilePath.ToString();
                // We patch both 3D Models AND Materials so textures keep their integrity across Options
                if ((localStr.EndsWith(".mdl", StringComparison.OrdinalIgnoreCase) ||
                     localStr.EndsWith(".mtrl", StringComparison.OrdinalIgnoreCase)) &&
                     !string.IsNullOrEmpty(oldModelId)) {

                    string patchedPath = PatchBinaryFile(fullModPath, localStr, oldModelId, targetModelId);
                    dict[newGamePath] = patchedPath;
                } else {
                    dict[newGamePath] = localFilePath;
                }
            } else {
                dict[newGamePath] = localFilePath;
            }

            hasChanged = true;
        }

        return hasChanged;
    }

    private string PatchBinaryFile(string fullModPath, string localFilePath, string oldModelId, string newModelId) {
        string absoluteOriginalPath = Path.Combine(fullModPath, localFilePath);
        if (!File.Exists(absoluteOriginalPath)) {
            return localFilePath;
        }

        string extension = Path.GetExtension(localFilePath);
        string newLocalPath = localFilePath.Replace(extension, $"_{newModelId}_armoire{extension}", StringComparison.OrdinalIgnoreCase);
        string absolutePatchPath = Path.Combine(fullModPath, newLocalPath);

        try {
            byte[] fileBytes = File.ReadAllBytes(absoluteOriginalPath);
            byte[] searchBytes = System.Text.Encoding.ASCII.GetBytes(oldModelId.ToLowerInvariant());
            byte[] replaceBytes = System.Text.Encoding.ASCII.GetBytes(newModelId.ToLowerInvariant());

            for (int i = 0; i <= fileBytes.Length - searchBytes.Length; i++) {
                bool isMatch = true;
                for (int j = 0; j < searchBytes.Length; j++) {
                    if (fileBytes[i + j] != searchBytes[j]) {
                        isMatch = false;
                        break;
                    }
                }
                if (isMatch) {
                    for (int j = 0; j < replaceBytes.Length; j++) {
                        fileBytes[i + j] = replaceBytes[j];
                    }
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(absolutePatchPath)!);
            File.WriteAllBytes(absolutePatchPath, fileBytes);
            return newLocalPath;
        } catch (Exception ex) {
            this.pluginLog.Error(ex, $"[ModSwapper] Failed to binary patch {localFilePath}");
            return localFilePath;
        }
    }

    private bool IsPathMatchingSlot(string path, string slotKey) {
        string lowerPath = path.ToLowerInvariant();
        if (slotKey != "wpn" && slotKey != "sub" && slotKey != "custom" && slotKey != "unknown") {
            return lowerPath.Contains($"_{slotKey}.") ||
                   lowerPath.Contains($"_{slotKey}_");
        }
        if (slotKey == "wpn" || slotKey == "sub") {
            return lowerPath.Contains("chara/weapon/");
        }

        return false;
    }
}