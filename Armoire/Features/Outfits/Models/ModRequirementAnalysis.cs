namespace Armoire.Features.Outfits.Models;

/// <summary>
/// Represents the specific state of a required mod on the local user's machine.
/// </summary>
public enum ModRequirementStatus {
    Ready,                  // Installed, enabled, and no conflicts
    DisabledOrConflicting,  // Installed, but either disabled by the user or crushed by another mod
    Missing                 // Not found in Penumbra's directory at all
}

/// <summary>
/// Contains the result of the pre-import analysis for a single required mod.
/// </summary>
public class ModRequirementAnalysis {
    public OutfitModRequirement Requirement { get; set; } = new();
    public ModRequirementStatus Status { get; set; }
    public string DetailMessage { get; set; } = string.Empty;
}