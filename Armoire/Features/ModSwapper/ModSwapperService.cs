namespace Armoire.Features.ModSwapper;

using Armoire.Features.LocalScanner;
using Armoire.Features.ModSwapper.Engines;
using Armoire.Features.PenumbraIpc;
using Dalamud.Plugin.Services;
using System;
using System.IO;
using System.Linq;

public class ModSwapperService : IModSwapperService {
    private readonly IModScannerManager scannerManager;
    private readonly IPluginLog pluginLog;
    private readonly IPenumbraClient penumbraClient;
    private readonly ArmoireConfiguration configuration;
    private readonly IJsonMutationEngine jsonEngine;

    public ModSwapperService(
        IModScannerManager scannerManager,
        IPluginLog pluginLog,
        IPenumbraClient penumbraClient,
        ArmoireConfiguration configuration,
        IJsonMutationEngine jsonEngine) {

        this.scannerManager = scannerManager;
        this.pluginLog = pluginLog;
        this.penumbraClient = penumbraClient;
        this.configuration = configuration;
        this.jsonEngine = jsonEngine;
    }

    public bool PerformSwap(string modId, string slotKey, string targetModelId, string textureProviderModId = "") {
        bool mainSwap = ExecuteSingleSwap(modId, slotKey, targetModelId);
        bool texSwap = false;

        if (!string.IsNullOrWhiteSpace(textureProviderModId)) {
            this.pluginLog.Info($"[ModSwapper] Executing secondary texture inheritance swap for {textureProviderModId}");
            texSwap = ExecuteSingleSwap(textureProviderModId, slotKey, targetModelId);
        }

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

                // Delegate the heavy lifting to the JSON engine
                if (this.jsonEngine.MutateModConfig(jsonContent, fullModPath, slotKey, targetModelId, out string newJsonContent)) {

                    if (!File.Exists(backupPath)) {
                        File.Copy(configFile, backupPath);
                    }

                    File.WriteAllText(configFile, newJsonContent);
                    anyFilesChanged = true;
                }
            }

            if (anyFilesChanged) {
                if (!this.configuration.ModifiedMods.TryGetValue(targetModId, out var modEntry)) {
                    string modName = this.scannerManager.ModCache.TryGetValue(targetModId, out var cache)
                        ? cache.ModName : targetModId;

                    modEntry = new ModifiedModEntry { ModName = modName };
                    this.configuration.ModifiedMods[targetModId] = modEntry;
                }

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

        // --- 1. CASCADING RESET FOR TEXTURE PROVIDERS ---
        // Before wiping the mod from memory, check if it forced any other mods to mutate (Texture Inheritance).
        // If so, we safely trigger a reset on them as well to ensure a perfectly clean state.
        if (this.configuration.ModifiedMods.TryGetValue(modId, out var modEntry)) {
            var linkedProviders = modEntry.TextureProviders.Values.Distinct().ToList();
            foreach (var providerId in linkedProviders) {
                if (!string.IsNullOrEmpty(providerId) && providerId != modId) {
                    this.pluginLog.Info($"[ModSwapper] Cascading reset to linked texture provider: {providerId}");
                    ResetMod(providerId); // Recursive call to clean the secondary mod
                }
            }
        }

        // --- 2. LOCAL MOD CLEANUP ---
        string fullModPath = Path.Combine(rootDir, modId);

        // Failsafe: if the directory doesn't exist anymore, just clear the config
        if (!Directory.Exists(fullModPath)) {
            if (this.configuration.ModifiedMods.Remove(modId)) {
                this.configuration.Save();
            }
            return false;
        }

        var backupFiles = Directory.GetFiles(fullModPath, "*.armoire_bak", SearchOption.TopDirectoryOnly);

        // If no backups exist, the mod is likely already clean, but we ensure the config is purged.
        if (backupFiles.Length == 0) {
            if (this.configuration.ModifiedMods.Remove(modId)) {
                this.configuration.Save();
            }
            return false;
        }

        try {
            // Restore original JSON configurations
            foreach (var backupPath in backupFiles) {
                string originalFilePath = backupPath.Replace(".armoire_bak", "");
                File.Copy(backupPath, originalFilePath, overwrite: true);
                File.Delete(backupPath);
            }

            // Clean up all generated patched files (models, materials, imc, etc.)
            var patchedFiles = Directory.GetFiles(fullModPath, "*_armoire.*", SearchOption.AllDirectories);
            foreach (var file in patchedFiles) {
                try {
                    File.Delete(file);
                } catch (Exception ex) {
                    this.pluginLog.Warning(ex, $"[ModSwapper] Failed to delete patched file: {file}");
                }
            }

            // Remove from memory
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
}