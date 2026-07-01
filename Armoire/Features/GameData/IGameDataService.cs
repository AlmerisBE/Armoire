namespace Armoire.Features.GameData;

public class ResolvedItem {
    public string Name { get; set; } = string.Empty;
    public uint IconId { get; set; }
    public string SlotKey { get; set; } = string.Empty;
    public uint ItemId { get; set; }
    public byte EquipLevel { get; set; }
    public uint ItemLevel { get; set; }
}

public interface IGameDataService {
    ResolvedItem ResolveItem(string gamePath);
}