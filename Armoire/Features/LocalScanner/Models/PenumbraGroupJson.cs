namespace Armoire.Features.LocalScanner.Models;

using System.Collections.Generic;

public class PenumbraGroupJson {
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Single"; // Can be "Single" (index) or "Multi" (bitmask)
    public List<PenumbraOptionJson> Options { get; set; } = new();
}

public class PenumbraOptionJson {
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Files { get; set; } = new();
    public Dictionary<string, string> FileSwaps { get; set; } = new();
}