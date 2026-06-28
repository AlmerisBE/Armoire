namespace Armoire.Features.Penumbra.Core.Models;

using System.Collections.Generic;

/// <summary>
/// Represents the footprint of a mod cached by Armoire for fast conflict detection.
/// </summary>
public class ArmoireModCacheEntry {
    // The unique identifier of the mod (usually its directory name on the disk)
    public string DirectoryName { get; set; } = string.Empty;

    // The human-readable name of the mod (extracted from meta.json)
    public string ModName { get; set; } = string.Empty;

    // A HashSet for O(1) lookup performance. 
    // Contains all the game paths (e.g., "chara/equipment/e6116/...") that this mod attempts to replace.
    public HashSet<string> ModifiedGamePaths { get; set; } = new(System.StringComparer.OrdinalIgnoreCase);
}