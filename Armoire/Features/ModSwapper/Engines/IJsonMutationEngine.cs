namespace Armoire.Features.ModSwapper.Engines;

public interface IJsonMutationEngine {
    /// <summary>
    /// Parses a Penumbra JSON configuration, safely modifies internal paths and cross-slot swaps,
    /// and triggers physical binary/IMC patching when required.
    /// </summary>
    /// <returns>True if the JSON was modified, along with the newly serialized JSON string.</returns>
    bool MutateModConfig(string originalJson, string fullModPath, string slotKey, string targetModelId, out string modifiedJson);
}