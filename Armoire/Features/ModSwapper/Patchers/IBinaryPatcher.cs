namespace Armoire.Features.ModSwapper.Patchers;

public interface IBinaryPatcher {
    /// <summary>
    /// Duplicates a binary file, replaces the internal ASCII model ID, and saves it with a new name.
    /// </summary>
    /// <returns>The relative path to the newly patched file, or the original path if patching failed.</returns>
    string PatchBinaryFile(string fullModPath, string localFilePath, string oldModelId, string targetModelId);
}