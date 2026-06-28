namespace Armoire.Features.Penumbra.Interfaces;

using Armoire.Features.Penumbra.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IModScannerManager {
    ScanState State { get; }
    int TotalMods { get; }
    int ProcessedMods { get; }
    int ErrorCount { get; }

    IReadOnlyDictionary<string, ArmoireModCacheEntry> ModCache { get; }

    Task StartScanAsync();
    void PauseScan();
    void ResumeScan();
    void CancelScan();
}