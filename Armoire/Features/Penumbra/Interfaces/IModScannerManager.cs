namespace Armoire.Features.Penumbra.Interfaces;

using Armoire.Features.Penumbra.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IModScannerManager {
    // Indicates if a background scan is currently running
    bool IsScanning { get; }

    // The in-memory cache of all installed mods and their affected paths
    IReadOnlyDictionary<string, ArmoireModCacheEntry> ModCache { get; }

    // Triggers an asynchronous scan of the Penumbra mods directory
    Task ScanModsAsync();
}