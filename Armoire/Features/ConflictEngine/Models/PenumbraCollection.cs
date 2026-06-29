namespace Armoire.Features.ConflictEngine.Models;

using System.Collections.Generic;

public class PenumbraCollection {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> ParentIds { get; set; } = new();

    public Dictionary<string, PenumbraMod> LocalSettings { get; set; } = new();
}