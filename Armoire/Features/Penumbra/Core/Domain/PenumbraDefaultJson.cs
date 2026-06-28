namespace Armoire.Features.Penumbra.Core.Domain;

using System.Collections.Generic;

/// <summary>
/// Maps the structure of the 'default_mod.json' file located at the root of a Penumbra mod.
/// </summary>
public class PenumbraDefaultModJson {
    // Standard file replacements
    public Dictionary<string, string> Files { get; set; } = new();

    // Internal game path swaps (e.g., using glove A's model for glove B)
    public Dictionary<string, string> FileSwaps { get; set; } = new();
}