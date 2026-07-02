namespace Armoire.Features.ConflictEngine.Models;

using System.Collections.Generic;

public class EffectiveCollectionState {
    public List<string> HierarchyNames { get; set; } = [];
    public Dictionary<string, PenumbraMod> EffectiveMods { get; set; } = [];

    /// <summary>
    /// Maps a specific game file path (e.g., "chara/equipment/e0123/...") to the ID of the Mod that ultimately controls it.
    /// </summary>
    public Dictionary<string, string> FileOwnership { get; set; } = new(System.StringComparer.OrdinalIgnoreCase);

    public int ConflictModCount { get; set; }
    public List<PenumbraMod> ConflictingMods { get; set; } = [];
}