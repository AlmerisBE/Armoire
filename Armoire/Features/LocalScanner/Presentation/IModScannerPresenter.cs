namespace Armoire.Features.LocalScanner.Presentation;

public interface IModScannerPresenter {
    // --- STATE DATA ---
    bool IsVisible { get; set; }
    bool IsScanning { get; }
    bool IsPaused { get; }
    int ScannedCount { get; }
    int TotalCount { get; }
    int IgnoredErrorsCount { get; }

    // --- ACTIONS ---
    void Open();
    void StartScan();
    void PauseScan();
    void ResumeScan();
    void CancelScan();
    void CloseToBackground();
}