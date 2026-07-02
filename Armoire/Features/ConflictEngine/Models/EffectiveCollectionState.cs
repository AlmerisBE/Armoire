namespace Armoire.Features.ConflictEngine.Models;

using System.Collections.Generic;

public class EffectiveCollectionState {
    public List<string> HierarchyNames { get; set; } = [];
    public Dictionary<string, PenumbraMod> EffectiveMods { get; set; } = [];

    /// <summary>
    /// Maps a specific game file path to the IDs of the Mods that attempt to control it.
    /// In case of equal priority conflicts, multiple Mod IDs will be present for a single path.
    /// </summary>
    public Dictionary<string, HashSet<string>> FileOwnership { get; set; } = new(System.StringComparer.OrdinalIgnoreCase);

    public int ConflictModCount { get; set; }
    public List<PenumbraMod> ConflictingMods { get; set; } = [];
}