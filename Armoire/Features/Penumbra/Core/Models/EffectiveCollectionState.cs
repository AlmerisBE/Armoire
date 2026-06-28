namespace Armoire.Features.Penumbra.Core.Models;

using Armoire.Features.Penumbra.Core.Domain;
using System.Collections.Generic;

public class EffectiveCollectionState {
    public List<string> HierarchyNames { get; set; } = new();
    public Dictionary<string, PenumbraMod> EffectiveMods { get; set; } = new();
    public int ConflictModCount { get; set; }
}