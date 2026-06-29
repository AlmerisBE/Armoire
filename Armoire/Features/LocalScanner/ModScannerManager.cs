namespace Armoire.Features.LocalScanner;

using Armoire.Features.LocalScanner.Models;
using Armoire.Features.PenumbraIpc;
using Dalamud.Interface.ImGuiNotification;
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

    private ConcurrentDictionary<string, ArmoireModCacheEntry> modCache = new(StringComparer.OrdinalIgnoreCase);
    private FileSystemWatcher? watcher;

    private int totalMods = 0;
    private int processedMods = 0;
    private int errorCount = 0;

    private CancellationTokenSource? cancellationTokenSource;
    private readonly ManualResetEventSlim pauseEvent = new(true);
    private Dictionary<string, string> modsToScan = new(StringComparer.OrdinalIgnoreCase);

    public ScanState State { get; private set; } = ScanState.Idle;
    public int TotalMods => this.totalMods;
    public int ProcessedMods => this.processedMods;
    public int ErrorCount => this.errorCount;

    public IReadOnlyDictionary<string, ArmoireModCacheEntry> ModCache => this.modCache;

    public event Action? OnCacheUpdated;

    public ModScannerManager(IPenumbraClient penumbraClient, IPluginLog pluginLog, INotificationManager notificationManager) {
        this.penumbraClient = penumbraClient;
        this.pluginLog = pluginLog;
        this.notificationManager = notificationManager;
    }

    public void InitializeScanProgress(int ipcModCount) {
        if (this.State != ScanState.Idle) {
            return;
        }

        this.modsToScan = this.penumbraClient.GetRawModsList();
        this.totalMods = this.modsToScan.Count > 0 ? this.modsToScan.Count : ipcModCount;
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

        if (this.modsToScan.Count == 0) {
            this.modsToScan = this.penumbraClient.GetRawModsList();
        }

        this.totalMods = this.modsToScan.Count;
        this.processedMods = 0;
        this.errorCount = 0;
        this.State = ScanState.Scanning;

        this.cancellationTokenSource = new CancellationTokenSource();
        this.pauseEvent.Set();

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var token = this.cancellationTokenSource.Token;

        try {
            await Task.Run(() => {
                var parallelOptions = new ParallelOptions {
                    CancellationToken = token,
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };

                var newCache = new ConcurrentDictionary<string, ArmoireModCacheEntry>(StringComparer.OrdinalIgnoreCase);

                Parallel.ForEach(this.modsToScan, parallelOptions, kvp => {
                    this.pauseEvent.Wait(token);
                    var entry = ProcessSingleMod(modDirectory, kvp.Key, kvp.Value, jsonOptions);

                    if (entry != null) {
                        newCache[kvp.Key] = entry;
                    } else {
                        Interlocked.Increment(ref this.errorCount);
                    }

                    Interlocked.Increment(ref this.processedMods);
                });

                this.modCache = newCache;
            }, token);

            SendNotification("Scan Complete", $"Successfully scanned {this.ProcessedMods} mods. Errors: {this.ErrorCount}", NotificationType.Success);

            this.OnCacheUpdated?.Invoke();

            EnableRealTimeMonitoring();

        } catch (OperationCanceledException) {
            this.pluginLog.Info("[ModScannerManager] Scan was canceled by the user.");
            SendNotification("Scan Canceled", $"Scan aborted at {this.ProcessedMods}/{this.TotalMods}.", NotificationType.Warning);
        } catch (Exception ex) {
            this.pluginLog.Error(ex, "[ModScannerManager] Fatal error during scan.");
            SendNotification("Scan Failed", "An unexpected error occurred. Check logs.", NotificationType.Error);
        } finally {
            ResetState();
            this.modsToScan.Clear();
        }
    }

    private ArmoireModCacheEntry? ProcessSingleMod(string rootDir, string dirName, string fallbackName, JsonSerializerOptions options) {
        var fullDirPath = Path.Combine(rootDir, dirName);
        if (!Directory.Exists(fullDirPath)) {
            return null;
        }

        var entry = new ArmoireModCacheEntry {
            DirectoryName = dirName,
            ModName = string.IsNullOrEmpty(fallbackName) ? dirName : fallbackName
        };

        try {
            // 1. Read default paths
            var defaultModPath = Path.Combine(fullDirPath, "default_mod.json");
            if (File.Exists(defaultModPath)) {
                var defaultData = JsonSerializer.Deserialize<PenumbraDefaultModJson>(File.ReadAllText(defaultModPath), options);
                if (defaultData != null) {
                    foreach (var path in defaultData.Files.Keys) {
                        entry.ModifiedGamePaths.Add(path);
                    }

                    foreach (var path in defaultData.FileSwaps.Keys) {
                        entry.ModifiedGamePaths.Add(path);
                    }
                }
            }

            // 2. NEW: Read option groups
            var groupFiles = Directory.GetFiles(fullDirPath, "group_*.json");
            foreach (var groupFile in groupFiles) {
                try {
                    var groupData = JsonSerializer.Deserialize<PenumbraGroupJson>(File.ReadAllText(groupFile), options);
                    if (groupData != null && !string.IsNullOrEmpty(groupData.Name)) {
                        var armoireGroup = new ArmoireOptionGroup { Type = groupData.Type };

                        foreach (var option in groupData.Options) {
                            var optionPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            if (option.Files != null) {
                                foreach (var path in option.Files.Keys) {
                                    optionPaths.Add(path);
                                }
                            }

                            if (option.FileSwaps != null) {
                                foreach (var path in option.FileSwaps.Keys) {
                                    optionPaths.Add(path);
                                }
                            }

                            armoireGroup.OptionPaths.Add(optionPaths);
                        }

                        entry.OptionGroups[groupData.Name] = armoireGroup;
                    }
                } catch { /* Ignore corrupted group files */ }
            }

            // 3. Read human-readable name if missing
            if (string.IsNullOrEmpty(fallbackName)) {
                var metaPath = Path.Combine(fullDirPath, "meta.json");
                if (File.Exists(metaPath)) {
                    var metaData = JsonSerializer.Deserialize<PenumbraMetaJson>(File.ReadAllText(metaPath), options);
                    if (metaData != null) {
                        entry.ModName = metaData.Name;
                    }
                }
            }
            return entry;
        } catch {
            return null;
        }
    }

    public void EnableRealTimeMonitoring() {
        if (this.watcher != null) {
            return;
        }

        var modDirectory = this.penumbraClient.GetModDirectory();
        if (string.IsNullOrEmpty(modDirectory) || !Directory.Exists(modDirectory)) {
            return;
        }

        this.watcher = new FileSystemWatcher(modDirectory) {
            NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        this.watcher.Changed += OnModFileSystemChanged;
        this.watcher.Created += OnModFileSystemChanged;
        this.watcher.Deleted += OnModFileSystemChanged;
        this.watcher.Renamed += OnModFileSystemRenamed;
    }

    private void OnModFileSystemChanged(object sender, FileSystemEventArgs e) => HandleFileSystemEvent(e.FullPath, e.ChangeType);
    private void OnModFileSystemRenamed(object sender, RenamedEventArgs e) {
        HandleFileSystemEvent(e.OldFullPath, WatcherChangeTypes.Deleted);
        HandleFileSystemEvent(e.FullPath, WatcherChangeTypes.Created);
    }

    private void HandleFileSystemEvent(string fullPath, WatcherChangeTypes changeType) {
        bool isDirectory = Directory.Exists(fullPath);
        bool isTargetJson = fullPath.EndsWith("default_mod.json", StringComparison.OrdinalIgnoreCase) ||
                            fullPath.EndsWith("meta.json", StringComparison.OrdinalIgnoreCase);

        if (!isDirectory && !isTargetJson && changeType != WatcherChangeTypes.Deleted) {
            return;
        }

        var modDirectory = this.penumbraClient.GetModDirectory();
        var relativePath = Path.GetRelativePath(modDirectory, fullPath);
        var dirName = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];

        if (string.IsNullOrEmpty(dirName) || dirName == ".") {
            return;
        }

        _ = Task.Run(() => {
            try {
                if (changeType == WatcherChangeTypes.Deleted && !Directory.Exists(Path.Combine(modDirectory, dirName))) {
                    if (this.modCache.TryRemove(dirName, out _)) {
                        this.OnCacheUpdated?.Invoke();
                    }
                } else {
                    Thread.Sleep(200);
                    var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var newEntry = ProcessSingleMod(modDirectory, dirName, string.Empty, jsonOptions);

                    if (newEntry != null) {
                        this.modCache[dirName] = newEntry;
                        this.OnCacheUpdated?.Invoke();
                    }
                }
            } catch { /* Ignore */ }
        });
    }

    public void PauseScan() { if (this.State == ScanState.Scanning) { this.pauseEvent.Reset(); this.State = ScanState.Paused; } }
    public void ResumeScan() { if (this.State == ScanState.Paused) { this.pauseEvent.Set(); this.State = ScanState.Scanning; } }
    public void CancelScan() {
        if (this.State != ScanState.Idle) {
            this.cancellationTokenSource?.Cancel();
        }
    }
    private void ResetState() { this.State = ScanState.Idle; this.cancellationTokenSource?.Dispose(); this.cancellationTokenSource = null; }
    private void SendNotification(string title, string content, NotificationType type) => this.notificationManager.AddNotification(new Notification { Title = title, Content = content, Type = type });

    public void Dispose() {
        CancelScan();
        this.pauseEvent.Dispose();
        if (this.watcher != null) {
            this.watcher.EnableRaisingEvents = false;
            this.watcher.Changed -= OnModFileSystemChanged;
            this.watcher.Created -= OnModFileSystemChanged;
            this.watcher.Deleted -= OnModFileSystemChanged;
            this.watcher.Renamed -= OnModFileSystemRenamed;
            this.watcher.Dispose();
        }
    }
}