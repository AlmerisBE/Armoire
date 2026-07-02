namespace Armoire.Features.Outfits.Models;

/// <summary>
/// Represents a snapshot of a specific equipment piece worn when the outfit was created.
/// </summary>
public class OutfitEquipmentPiece {
    public uint ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public uint IconId { get; set; }
    public byte EquipLevel { get; set; }
    public uint ItemLevel { get; set; }
    public string Category { get; set; } = string.Empty;
    /// <summary>
    /// If true, this equipment piece will be ignored during activation and greyed out in the UI.
    /// </summary>
    public bool IsIgnored { get; set; } = false;

    /// <summary>
    /// The names of the mods that are actively modifying this specific piece.
    /// </summary>
    public string ModifyingModNames { get; set; } = string.Empty;
}