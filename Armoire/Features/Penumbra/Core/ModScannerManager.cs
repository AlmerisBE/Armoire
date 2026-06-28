namespace Armoire.Features.Penumbra.Core;

using Armoire.Features.Penumbra.Core.Domain;
using Armoire.Features.Penumbra.Core.Models;
using Armoire.Features.Penumbra.Interfaces;
using Dalamud.Interface.ImGuiNotification; // For Notification configuration
using Dalamud.Plugin.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class ModScannerManager : IModScannerManager, IDisposable {
    private readonly IPenumbraClient penumbraClient;
    private readonly IPluginLog pluginLog;
    private readonly INotificationManager notificationManager;

    private Dictionary<string, ArmoireModCacheEntry> modCache = new();

    private int totalMods = 0;
    private int processedMods = 0;
    private int errorCount = 0;

    // Thread control mechanisms
    private CancellationTokenSource? cancellationTokenSource;
    private readonly ManualResetEventSlim pauseEvent = new(true); // true = running (green light)

    public ScanState State { get; private set; } = ScanState.Idle;
    public int TotalMods => this.totalMods;
    public int ProcessedMods => this.processedMods;
    public int ErrorCount => this.errorCount;

    public IReadOnlyDictionary<string, ArmoireModCacheEntry> ModCache => this.modCache;

    public ModScannerManager(IPenumbraClient penumbraClient, IPluginLog pluginLog, INotificationManager notificationManager) {
        this.penumbraClient = penumbraClient;
        this.pluginLog = pluginLog;
        this.notificationManager = notificationManager;
    }

    // Track the temporary list provided by the UI or the IPC configuration
    private Dictionary<string, string> modsToScan = new();

    public void InitializeScanProgress(int ipcModCount) {
        if (this.State != ScanState.Idle) {
            return;
        }

        // Fetch the list right away to store targets and get accurate count
        this.modsToScan = this.penumbraClient.GetRawModsList();

        this.totalMods = this.modsToScan.Count;
        this.processedMods = 0;
        this.errorCount = 0;
    }

    public async Task StartScanAsync() {
        if (this.State != ScanState.Idle) {
            return;
        }

        var modDirectory = this.penumbraClient.GetModDirectory();
        if (string.IsNullOrEmpty(modDirectory) || !Directory.Exists(modDirectory)) {
            SendNotification("Scanner Error", "Penumbra mod directory not found.", NotificationType.Error);
            return;
        }

        // If the user didn't open the window but hit scan via another trigger, ensure list is populated
        if (this.modsToScan.Count == 0) {
            this.modsToScan = this.penumbraClient.GetRawModsList();
        }

        this.totalMods = this.modsToScan.Count;
        this.processedMods = 0;
        this.errorCount = 0;
        this.State = ScanState.Scanning;

        this.cancellationTokenSource = new CancellationTokenSource();
        this.pauseEvent.Set();

        var newCache = new ConcurrentDictionary<string, ArmoireModCacheEntry>();
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var token = this.cancellationTokenSource.Token;

        try {
            await Task.Run(() => {
                var parallelOptions = new ParallelOptions {
                    CancellationToken = token,
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };

                // Loop through our targeted IPC dictionary instead of directory scanning
                Parallel.ForEach(this.modsToScan, parallelOptions, kvp => {
                    this.pauseEvent.Wait(token);

                    var dirName = kvp.Key;   // Folder name from Penumbra
                    var modName = kvp.Value; // Human name from Penumbra API (No meta.json read needed!)

                    var entry = new ArmoireModCacheEntry {
                        DirectoryName = dirName,
                        ModName = string.IsNullOrEmpty(modName) ? dirName : modName
                    };

                    var fullDirPath = Path.Combine(modDirectory, dirName);
                    bool hasError = false;

                    if (Directory.Exists(fullDirPath)) {
                        try {
                            // We only read default_mod.json to fetch game paths
                            var defaultModPath = Path.Combine(fullDirPath, "default_mod.json");
                            if (File.Exists(defaultModPath)) {
                                var defaultData = JsonSerializer.Deserialize<PenumbraDefaultModJson>(File.ReadAllText(defaultModPath), jsonOptions);
                                if (defaultData != null) {
                                    foreach (var path in defaultData.Files.Keys) {
                                        entry.ModifiedGamePaths.Add(path);
                                    }

                                    foreach (var path in defaultData.FileSwaps.Keys) {
                                        entry.ModifiedGamePaths.Add(path);
                                    }
                                }
                            }
                        } catch {
                            hasError = true;
                        }
                    } else {
                        hasError = true; // Directory registered in Penumbra but missing on disk
                    }

                    if (hasError) {
                        Interlocked.Increment(ref this.errorCount);
                    }

                    newCache[dirName] = entry;

                    Interlocked.Increment(ref this.processedMods);
                });
            }, token);

            this.modCache = new Dictionary<string, ArmoireModCacheEntry>(newCache);
            SendNotification("Scan Complete", $"Successfully scanned {this.ProcessedMods} mods. Errors: {this.ErrorCount}", NotificationType.Success);

        } catch (OperationCanceledException) {
            this.pluginLog.Info("[ModScannerManager] Scan was canceled by the user.");
            SendNotification("Scan Canceled", $"Scan aborted at {this.ProcessedMods}/{this.TotalMods}.", NotificationType.Warning);
        } catch (Exception ex) {
            this.pluginLog.Error(ex, "[ModScannerManager] Fatal error during scan.");
            SendNotification("Scan Failed", "An unexpected error occurred. Check logs.", NotificationType.Error);
        } finally {
            ResetState();
            this.modsToScan.Clear(); // Clear working memory list
        }
    }

    public void PauseScan() {
        if (this.State != ScanState.Scanning) {
            return;
        }

        this.pauseEvent.Reset(); // Turn light to red
        this.State = ScanState.Paused;
    }

    public void ResumeScan() {
        if (this.State != ScanState.Paused) {
            return;
        }

        this.pauseEvent.Set(); // Turn light to green
        this.State = ScanState.Scanning;
    }

    public void CancelScan() {
        if (this.State == ScanState.Idle) {
            return;
        }

        this.cancellationTokenSource?.Cancel();
    }

    private void ResetState() {
        this.State = ScanState.Idle;
        this.cancellationTokenSource?.Dispose();
        this.cancellationTokenSource = null;
    }

    private void SendNotification(string title, string content, NotificationType type) {
        this.notificationManager.AddNotification(new Notification {
            Title = title,
            Content = content,
            Type = type,
            Minimized = false
        });
    }

    public void Dispose() {
        CancelScan();
        this.pauseEvent.Dispose();
    }
}