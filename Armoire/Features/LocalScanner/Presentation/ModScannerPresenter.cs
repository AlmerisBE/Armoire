namespace Armoire.Features.LocalScanner.Presentation;

using Armoire.Features.LocalScanner;

public class ModScannerPresenter : IModScannerPresenter {
    private readonly IModScannerManager scannerManager;

    public bool IsVisible { get; set; } = false;

    // Map UI states directly to the underlying business manager
    public bool IsScanning => this.scannerManager.IsScanning;
    public bool IsPaused => this.scannerManager.IsPaused;
    public int ScannedCount => this.scannerManager.ScannedModsCount;
    public int TotalCount => this.scannerManager.TotalModsCount;
    public int IgnoredErrorsCount => this.scannerManager.IgnoredErrorsCount;

    public ModScannerPresenter(IModScannerManager scannerManager) {
        this.scannerManager = scannerManager;
    }

    public void Open() {
        this.IsVisible = true;
    }

    public void StartScan() {
        // Assuming your manager has a StartScan method
        this.scannerManager.StartFullScan();
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