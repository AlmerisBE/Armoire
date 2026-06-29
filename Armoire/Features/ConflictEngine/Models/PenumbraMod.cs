namespace Armoire.Features.ConflictEngine.Models;

using System.Collections.Generic;

public class PenumbraMod {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int Priority { get; set; }
    public string SourceCollectionName { get; set; } = string.Empty;

    public Dictionary<string, uint> Settings { get; set; } = new(System.StringComparer.OrdinalIgnoreCase);

    public HashSet<string> OverwrittenBy { get; set; } = new(System.StringComparer.OrdinalIgnoreCase);

    public HashSet<string> ConflictingSlots { get; set; } = new(System.StringComparer.OrdinalIgnoreCase);
}