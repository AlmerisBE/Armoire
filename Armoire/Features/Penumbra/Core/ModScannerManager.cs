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

    public void InitializeScanProgress(int ipcModCount) {
        if (this.State != ScanState.Idle) {
            return;
        }

        this.totalMods = ipcModCount;
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

        var directories = Directory.GetDirectories(modDirectory);
        this.totalMods = directories.Length;
        this.processedMods = 0;
        this.errorCount = 0;
        this.State = ScanState.Scanning;

        this.cancellationTokenSource = new CancellationTokenSource();
        this.pauseEvent.Set(); // Ensure the light is green

        var newCache = new ConcurrentDictionary<string, ArmoireModCacheEntry>();
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var token = this.cancellationTokenSource.Token;

        try {
            await Task.Run(() => {
                var parallelOptions = new ParallelOptions {
                    CancellationToken = token,
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };

                Parallel.ForEach(directories, parallelOptions, dir => {
                    // Block the thread here if pauseEvent.Reset() was called by the user
                    this.pauseEvent.Wait(token);

                    var dirName = Path.GetFileName(dir);
                    var entry = new ArmoireModCacheEntry { DirectoryName = dirName };
                    bool hasError = false;

                    try {
                        var metaPath = Path.Combine(dir, "meta.json");
                        if (File.Exists(metaPath)) {
                            var metaData = JsonSerializer.Deserialize<PenumbraMetaJson>(File.ReadAllText(metaPath), jsonOptions);
                            if (metaData != null) {
                                entry.ModName = metaData.Name;
                            }
                        }

                        if (string.IsNullOrEmpty(entry.ModName)) {
                            entry.ModName = dirName;
                        }

                        var defaultModPath = Path.Combine(dir, "default_mod.json");
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

                    if (hasError) {
                        Interlocked.Increment(ref this.errorCount);
                    }

                    newCache[dirName] = entry;

                    Interlocked.Increment(ref this.processedMods);
                });
            }, token);

            // If we reach this point, scan completed successfully
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