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

    /// <summary>
    /// Translates a game ItemId into its corresponding 3D Model ID (e.g., "e0123" or "w0050").
    /// Returns an empty string if the item is not found or has no model.
    /// </summary>
    string GetModelIdFromItemId(uint itemId);

    /// <summary>
    /// Retrieves full item details directly from its ItemId.
    /// </summary>
    ResolvedItem? GetItemInfo(uint itemId);
}