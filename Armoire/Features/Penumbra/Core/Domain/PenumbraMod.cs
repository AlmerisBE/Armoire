namespace Armoire.Features.Penumbra.Core.Domain;

public class PenumbraMod {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int Priority { get; set; }
    public string SourceCollectionName { get; set; } = string.Empty;
}