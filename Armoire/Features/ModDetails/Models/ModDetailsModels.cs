namespace Armoire.Features.ModDetails.Models;

using System.Collections.Generic;

public class DetailedSlotState {
    public List<string> AffectedPaths { get; set; } = [];
    public string LocalizedItemName { get; set; } = string.Empty;
    public uint IconId { get; set; }
    public string SlotCategory { get; set; } = string.Empty;
    public bool IsConflicting { get; set; }
    public List<string> OverwrittenByMods { get; set; } = [];
    public uint ItemId { get; set; }
}

public class DetailedModState {
    public string ModId { get; set; } = string.Empty;
    public string ModName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int Priority { get; set; }
    public List<DetailedSlotState> ReplacedSlots { get; set; } = [];
}