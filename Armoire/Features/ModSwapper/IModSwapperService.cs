namespace Armoire.Features.ModSwapper;

public interface IModSwapperService {
    /// <summary>
    /// Performs a technical swap by modifying the mod's configuration files on disk.
    /// Redirects all equipment files from the original model to a new target model.
    /// </summary>
    /// <param name="modId">The unique ID of the mod to modify.</param>
    /// <param name="slotKey">The slot being swapped (e.g., "top", "met").</param>
    /// <param name="targetModelId">The new vanilla model ID to target (e.g., "e0500").</param>
    bool PerformSwap(string modId, string slotKey, string targetModelId);

    /// <summary>
    /// Restores the mod to its original state by removing all generated swaps.
    /// </summary>
    bool ResetMod(string modId);
}