namespace Armoire.Features.Outfits.Models;

/// <summary>
/// Represents a specific mod required by an Armoire outfit.
/// Contains enough metadata to help the user find the mod if it's missing.
/// </summary>
public class OutfitModRequirement {
    /// <summary>
    /// The internal directory name or ID of the mod in Penumbra.
    /// Crucial for technical matching during the import process.
    /// </summary>
    public string ModId { get; set; } = string.Empty;

    /// <summary>
    /// The human-readable name of the mod (from meta.json).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The author of the mod. Highly useful for users trying to find a missing mod online.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// The version of the mod at the time the outfit was saved.
    /// Allows warning the user if their installed mod is drastically outdated.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// If true, this mod is intentionally ignored by the user. It won't trigger warnings and won't show up in the equipment table.
    /// </summary>
    public bool IsIgnored { get; set; } = false;

}