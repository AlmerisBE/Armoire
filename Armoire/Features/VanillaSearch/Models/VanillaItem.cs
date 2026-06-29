namespace Armoire.Features.VanillaSearch.Models;

public class VanillaItem {
    public uint ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public ushort IconId { get; set; }
    public string ExpansionName { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
}