namespace Armoire.Features.Penumbra.Core;

using Armoire.Features.Penumbra.Core.Domain;
using Armoire.Features.Penumbra.Core.Models;
using Armoire.Features.Penumbra.Interfaces;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

public class ModScannerManager : IModScannerManager {
    private readonly IPenumbraClient penumbraClient;
    private readonly IPluginLog pluginLog;
    private Dictionary<string, ArmoireModCacheEntry> modCache = new();
    private bool isScanning = false;

    public bool IsScanning => this.isScanning;
    public IReadOnlyDictionary<string, ArmoireModCacheEntry> ModCache => this.modCache;

    public ModScannerManager(IPenumbraClient penumbraClient, IPluginLog pluginLog) {
        this.penumbraClient = penumbraClient;
        this.pluginLog = pluginLog;
    }

    public async Task ScanModsAsync() {
        if (this.isScanning) {
            return;
        }

        this.isScanning = true;

        try {
            await Task.Run(() => {
                var modDirectory = this.penumbraClient.GetModDirectory();

                if (string.IsNullOrEmpty(modDirectory) || !Directory.Exists(modDirectory)) {
                    this.pluginLog.Warning("[ModScannerManager] Penumbra mod directory is not found or empty.");
                    return;
                }

                this.pluginLog.Info($"[ModScannerManager] Starting parallel scan of mods in: {modDirectory}");

                var directories = Directory.GetDirectories(modDirectory);
                var newCache = new ConcurrentDictionary<string, ArmoireModCacheEntry>();
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // Use parallel processing to parse thousands of JSON files instantly
                Parallel.ForEach(directories, dir => {
                    var dirName = Path.GetFileName(dir);
                    var entry = new ArmoireModCacheEntry {
                        DirectoryName = dirName
                    };

                    // 1. Parse meta.json for the human-readable name
                    var metaPath = Path.Combine(dir, "meta.json");
                    if (File.Exists(metaPath)) {
                        try {
                            var metaJson = File.ReadAllText(metaPath);
                            var metaData = JsonSerializer.Deserialize<PenumbraMetaJson>(metaJson, jsonOptions);
                            if (metaData != null) {
                                entry.ModName = metaData.Name;
                            }
                        } catch {
                            // Silently ignore corrupted meta.json files
                        }
                    }

                    // Fallback to directory name if meta.json is missing or unnamed
                    if (string.IsNullOrEmpty(entry.ModName)) {
                        entry.ModName = dirName;
                    }

                    // 2. Parse default_mod.json for replaced game paths
                    var defaultModPath = Path.Combine(dir, "default_mod.json");
                    if (File.Exists(defaultModPath)) {
                        try {
                            var defaultJson = File.ReadAllText(defaultModPath);
                            var defaultData = JsonSerializer.Deserialize<PenumbraDefaultModJson>(defaultJson, jsonOptions);

                            if (defaultData != null) {
                                foreach (var gamePath in defaultData.Files.Keys) {
                                    entry.ModifiedGamePaths.Add(gamePath);
                                }
                                foreach (var gamePath in defaultData.FileSwaps.Keys) {
                                    entry.ModifiedGamePaths.Add(gamePath);
                                }
                            }
                        } catch {
                            // Silently ignore corrupted default_mod.json files
                        }
                    }

                    newCache[dirName] = entry;
                });

                // Safely swap the old cache with the newly built one
                this.modCache = new Dictionary<string, ArmoireModCacheEntry>(newCache);
                this.pluginLog.Info($"[ModScannerManager] Scan complete. {this.modCache.Count} mods indexed.");
            });
        } catch (Exception ex) {
            this.pluginLog.Error(ex, "[ModScannerManager] A fatal error occurred during the mod scanning process.");
        } finally {
            this.isScanning = false;
        }
    }
}