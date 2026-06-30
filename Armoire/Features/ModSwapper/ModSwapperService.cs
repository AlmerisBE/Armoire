namespace Armoire.Features.ModSwapper;

using Armoire.Features.LocalScanner;
using Armoire.Features.LocalScanner.Models;
using Armoire.Features.PenumbraIpc;
using Dalamud.Plugin.Services;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class ModSwapperService : IModSwapperService {
    private readonly IModScannerManager scannerManager;
    private readonly IPluginLog pluginLog;
    private readonly IPenumbraClient penumbraClient;

    private readonly Regex modelIdRegex = new Regex(@"([ew]\d{4})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ModSwapperService(IModScannerManager scannerManager, IPluginLog pluginLog, IPenumbraClient penumbraClient) {
        this.scannerManager = scannerManager;
        this.pluginLog = pluginLog;
        this.penumbraClient = penumbraClient;
    }

    public bool PerformSwap(string originalModId, string slotKey, string targetModelId) {
        string rootDir = this.penumbraClient.GetModDirectory();
        if (string.IsNullOrEmpty(rootDir)) {
            return false;
        }

        string originalModPath = Path.Combine(rootDir, originalModId);
        string patchModId = $"{originalModId}_armoire";
        string patchModPath = Path.Combine(rootDir, patchModId);

        if (!Directory.Exists(originalModPath)) {
            return false;
        }

        try {
            this.pluginLog.Info($"[ModSwapper] Generating Delta Patch for {originalModId} ({slotKey} -> {targetModelId})");

            // 1. Initialize the patch mod if it does not exist
            InitializePatchMod(originalModPath, patchModPath, originalModId);

            // 2. Find the original .mdl file to modify (by reading the scanner cache)
            if (!this.scannerManager.ModCache.TryGetValue(originalModId, out var cache)) {
                return false;
            }

            string originalMdlPath = FindMdlPathForSlot(cache, slotKey);
            if (string.IsNullOrEmpty(originalMdlPath)) {
                this.pluginLog.Warning($"[ModSwapper] No .mdl found for slot {slotKey} in original mod.");
                return false;
            }

            // 3. Patch the binary and copy it into the patch mod
            string patchedLocalMdlPath = PatchModelBinary(originalModPath, patchModPath, originalMdlPath, targetModelId);
            if (string.IsNullOrEmpty(patchedLocalMdlPath)) {
                return false;
            }

            // 4. Update the default_mod.json of the patch
            string newGameRoute = this.modelIdRegex.Replace(originalMdlPath, targetModelId);
            UpdatePatchJson(patchModPath, newGameRoute, patchedLocalMdlPath);

            // 5. Tell Penumbra to reload (it will detect the new folder)
            this.penumbraClient.ReloadMod(patchModId);

            // (Priority management via the Penumbra API will require an update to PenumbraClient)
            this.penumbraClient.RedrawAll();

            return true;
        } catch (Exception ex) {
            this.pluginLog.Error(ex, $"[ModSwapper] Failed to generate Delta Patch for {originalModId}");
            return false;
        }
    }

    public bool ResetMod(string originalModId) {
        string rootDir = this.penumbraClient.GetModDirectory();
        if (string.IsNullOrEmpty(rootDir)) {
            return false;
        }

        string patchModId = $"{originalModId}_armoire";
        string patchModPath = Path.Combine(rootDir, patchModId);

        if (!Directory.Exists(patchModPath)) {
            this.pluginLog.Warning($"[ModSwapper] No patch mod found for {originalModId}. Already clean.");
            return false;
        }

        try {
            // Complete and radical deletion of the generated Patch mod directory
            Directory.Delete(patchModPath, true);

            // Penumbra will clean its cache if the folder disappeared
            this.penumbraClient.ReloadMod(patchModId);
            this.penumbraClient.RedrawAll();
            return true;
        } catch (Exception ex) {
            this.pluginLog.Error(ex, $"[ModSwapper] Failed to delete patch mod for {originalModId}");
            return false;
        }
    }

    private void InitializePatchMod(string originalModPath, string patchModPath, string originalModId) {
        if (!Directory.Exists(patchModPath)) {
            Directory.CreateDirectory(patchModPath);
        }

        string metaPath = Path.Combine(patchModPath, "meta.json");
        if (!File.Exists(metaPath)) {
            // Retrieve the original mod's name to create the suffix
            string originalName = originalModId;
            string origMeta = Path.Combine(originalModPath, "meta.json");
            if (File.Exists(origMeta)) {
                try {
                    var metaObj = JObject.Parse(File.ReadAllText(origMeta));
                    originalName = metaObj["Name"]?.ToString() ?? originalModId;
                } catch { }
            }

            var patchMeta = new JObject {
                ["Name"] = $"{originalName} (Armoire Swaps)",
                ["Author"] = "Armoire Plugin",
                ["Description"] = "Auto-generated Delta Patch for item swapping. Keep original mod enabled!",
                ["Version"] = "1.0.0"
            };
            File.WriteAllText(metaPath, patchMeta.ToString());
        }

        string jsonPath = Path.Combine(patchModPath, "default_mod.json");
        if (!File.Exists(jsonPath)) {
            var defaultJson = new JObject {
                ["Files"] = new JObject(),
                ["FileSwaps"] = new JObject(),
                ["Manipulations"] = new JArray()
            };
            File.WriteAllText(jsonPath, defaultJson.ToString());
        }
    }

    private string FindMdlPathForSlot(ArmoireModCacheEntry cache, string slotKey) {
        var allPaths = cache.ModifiedGamePaths.Concat(cache.OptionGroups.Values.SelectMany(g => g.OptionPaths.SelectMany(p => p)));

        foreach (var path in allPaths) {
            string lower = path.ToLowerInvariant();
            if (lower.EndsWith(".mdl") && (lower.Contains($"_{slotKey}.") || lower.Contains($"_{slotKey}_") || (slotKey == "wpn" && lower.Contains("weapon")))) {
                return path;
            }
        }
        return string.Empty;
    }

    private string PatchModelBinary(string originalModPath, string patchModPath, string originalGamePath, string newModelId) {
        var match = this.modelIdRegex.Match(originalGamePath);
        if (!match.Success) {
            return string.Empty;
        }

        string oldModelId = match.Value;

        // Create a flat local path for the file in the patch mod
        string localMdlName = Path.GetFileName(originalGamePath).Replace(oldModelId, newModelId, StringComparison.OrdinalIgnoreCase);
        string absoluteOriginalMdl = Path.Combine(originalModPath, originalGamePath);
        string absolutePatchMdl = Path.Combine(patchModPath, localMdlName);

        // If the source file is not at the root (hidden in an option), scan the original folder recursively
        if (!File.Exists(absoluteOriginalMdl)) {
            var files = Directory.GetFiles(originalModPath, Path.GetFileName(originalGamePath), SearchOption.AllDirectories);
            if (files.Length > 0) {
                absoluteOriginalMdl = files[0];
            } else {
                return string.Empty;
            }
        }

        try {
            byte[] fileBytes = File.ReadAllBytes(absoluteOriginalMdl);
            byte[] searchBytes = System.Text.Encoding.ASCII.GetBytes(oldModelId.ToLowerInvariant());
            byte[] replaceBytes = System.Text.Encoding.ASCII.GetBytes(newModelId.ToLowerInvariant());

            for (int i = 0; i <= fileBytes.Length - searchBytes.Length; i++) {
                bool isMatch = true;
                for (int j = 0; j < searchBytes.Length; j++) {
                    if (fileBytes[i + j] != searchBytes[j]) { isMatch = false; break; }
                }
                if (isMatch) {
                    for (int j = 0; j < replaceBytes.Length; j++) {
                        fileBytes[i + j] = replaceBytes[j];
                    }
                }
            }

            File.WriteAllBytes(absolutePatchMdl, fileBytes);
            return localMdlName;
        } catch (Exception ex) {
            this.pluginLog.Error(ex, "Failed to patch binary.");
            return string.Empty;
        }
    }

    private void UpdatePatchJson(string patchModPath, string newGameRoute, string patchedLocalMdlPath) {
        string jsonPath = Path.Combine(patchModPath, "default_mod.json");
        try {
            var root = JObject.Parse(File.ReadAllText(jsonPath));
            if (root["Files"] is JObject files) {
                // Consolidation: add or overwrite the existing path for this slot
                files[newGameRoute] = patchedLocalMdlPath;
            }
            File.WriteAllText(jsonPath, root.ToString());
        } catch (Exception ex) {
            this.pluginLog.Error(ex, "Failed to update patch default_mod.json");
        }
    }
}