namespace Armoire.Features.Penumbra.Core.Models;

using System.Collections.Generic;

public class PenumbraStatusResult {
    public bool IsEnabled { get; set; }
    public bool IsPlayerConnected => !string.IsNullOrEmpty(PlayerName);
    public int ModCount { get; set; }
    public int CollectionTotalMods { get; set; }
    public int CollectionEnabledMods { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public List<string> ActiveCollections { get; set; } = new();
}