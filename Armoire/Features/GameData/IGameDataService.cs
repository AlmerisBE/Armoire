namespace Armoire.Features.GameData;

public class ResolvedItem {
    public string Name { get; set; } = string.Empty;
    public uint IconId { get; set; }
    public string SlotKey { get; set; } = string.Empty;
}

public interface IGameDataService {
    ResolvedItem ResolveItem(string gamePath);
}