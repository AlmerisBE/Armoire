namespace Armoire.Features.LocalScanner.Models;

/// <summary>
/// Maps the structure of the 'meta.json' file located at the root of a Penumbra mod.
/// </summary>
public class PenumbraMetaJson {
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}