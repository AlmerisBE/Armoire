namespace Armoire.Features.VanillaSearch.Models;

public class VanillaItem {
    public uint ItemId { get; set; }
    public string Name { get; set; } = string.Empty;

    // The internal model ID (e.g., "e0123" or "w0123")
    public string ModelId { get; set; } = string.Empty;

    // Will be used later to fetch the image via Dalamud's TextureProvider
    public ushort IconId { get; set; }
}