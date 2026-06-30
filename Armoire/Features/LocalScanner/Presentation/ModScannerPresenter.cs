namespace Armoire.Features.LocalScanner.Presentation;

using Armoire.Features.LocalScanner;
using Armoire.Features.LocalScanner.Models;

public class ModScannerPresenter : IModScannerPresenter {
    private readonly IModScannerManager scannerManager;

    public bool IsVisible { get; set; } = false;

    // Map UI states directly to the underlying business manager properties
    // Note: Adjust "ScanState.Scanning" if your enum uses a different name (like InProgress or Running)
    public bool IsScanning => this.scannerManager.State == ScanState.Scanning;
    public bool IsPaused => this.scannerManager.State == ScanState.Paused;

    public int ScannedCount => this.scannerManager.ProcessedMods;
    public int TotalCount => this.scannerManager.TotalMods;
    public int IgnoredErrorsCount => this.scannerManager.ErrorCount;

    public ModScannerPresenter(IModScannerManager scannerManager) {
        this.scannerManager = scannerManager;
    }

    public void Open() {
        this.IsVisible = true;
    }

    public void StartScan() {
        // Discard the Task using '_' so it runs in the background without blocking the UI thread
        _ = this.scannerManager.StartScanAsync();
    }

    public void PauseScan() {
        this.scannerManager.PauseScan();
    }

    public void ResumeScan() {
        this.scannerManager.ResumeScan();
    }

    public void CancelScan() {
        this.scannerManager.CancelScan();
        this.IsVisible = false;
    }

    public void CloseToBackground() {
        // Simply hide the window, the background task continues
        this.IsVisible = false;
    }
}