namespace Armoire.Features.LocalScanner;

using Armoire.Features.LocalScanner.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IModScannerManager {
    ScanState State { get; }
    int TotalMods { get; }
    int ProcessedMods { get; }
    int ErrorCount { get; }

    IReadOnlyDictionary<string, ArmoireModCacheEntry> ModCache { get; }

    event Action? OnCacheUpdated;

    void InitializeScanProgress(int ipcModCount);
    Task StartScanAsync();
    void PauseScan();
    void ResumeScan();
    void CancelScan();
    void EnableRealTimeMonitoring();
}