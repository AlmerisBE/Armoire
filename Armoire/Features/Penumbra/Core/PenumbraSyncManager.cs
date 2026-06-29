namespace Armoire.Features.Penumbra.Core;

using Armoire.Features.Penumbra.Core.Models;
using Armoire.Features.Penumbra.Interfaces;
using Dalamud.Plugin.Services;
using System;
using System.Threading.Tasks;

public class PenumbraSyncManager : IPenumbraSyncManager, IDisposable {
    private readonly IFramework framework;
    private readonly IPenumbraAnalyzer analyzer;
    private readonly IPenumbraClient penumbraClient;
    private readonly IPenumbraRepository repository;
    private readonly IObjectTable objectTable;
    private readonly IModScannerManager scannerManager;

    private string lastPlayerName = string.Empty;
    private int lastModCount = -1;
    private DateTime lastCheckTime = DateTime.MinValue;
    private bool pendingRefresh = false;

    public event Action<PenumbraStatusResult>? OnStatusUpdated;

    public PenumbraSyncManager(
        IFramework framework,
        IPenumbraAnalyzer analyzer,
        IPenumbraClient penumbraClient,
        IPenumbraRepository repository,
        IObjectTable objectTable,
        IModScannerManager scannerManager) {

        this.framework = framework;
        this.analyzer = analyzer;
        this.penumbraClient = penumbraClient;
        this.repository = repository;
        this.objectTable = objectTable;
        this.scannerManager = scannerManager;

        this.framework.Update += OnFrameworkUpdate;
        this.scannerManager.OnCacheUpdated += ForceRefresh;

        this.penumbraClient.OnInitialized += TriggerBackgroundScan;

        if (this.penumbraClient.IsEnabled()) {
            TriggerBackgroundScan();
        }
    }

    private void TriggerBackgroundScan() {
        if (this.scannerManager.State == ScanState.Idle) {
            this.scannerManager.InitializeScanProgress(this.penumbraClient.GetModsCount());
            _ = this.scannerManager.StartScanAsync();
        }
    }

    private void OnFrameworkUpdate(IFramework fw) {
        if (pendingRefresh) {
            OnStatusUpdated?.Invoke(this.analyzer.GetStatusReport());
            pendingRefresh = false;
        }

        if (this.repository.IsLoading) {
            return;
        }

        if ((DateTime.Now - lastCheckTime).TotalSeconds < 2.0) {
            return;
        }

        lastCheckTime = DateTime.Now;

        var currentPlayerName = this.objectTable.LocalPlayer?.Name.TextValue ?? string.Empty;
        var currentModCount = this.penumbraClient.GetRawModsList().Count;

        bool hasChanged = currentPlayerName != lastPlayerName || currentModCount != lastModCount;
        bool needsRetry = this.penumbraClient.IsEnabled() && !string.IsNullOrEmpty(currentPlayerName) && this.lastPlayerName == string.Empty;

        if (hasChanged || needsRetry || this.repository.IsStale) {
            lastPlayerName = currentPlayerName;
            lastModCount = currentModCount;

            _ = Task.Run(async () => {
                await this.repository.SyncDataAsync();
                pendingRefresh = true;
            });
        }
    }

    public void ForceRefresh() {
        this.repository.MarkStale();
        this.lastCheckTime = DateTime.MinValue;
    }

    public void Dispose() {
        this.framework.Update -= OnFrameworkUpdate;
        this.scannerManager.OnCacheUpdated -= ForceRefresh;
        this.penumbraClient.OnInitialized -= TriggerBackgroundScan;
    }
}