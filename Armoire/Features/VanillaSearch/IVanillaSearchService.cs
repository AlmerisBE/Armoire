namespace Armoire.Features.VanillaSearch;

using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.VanillaSearch.Models;
using System.Collections.Generic;

public interface IVanillaSearchService {
    /// <summary>
    /// Searches for vanilla items matching a specific equipment slot, 
    /// excluding any models that are currently modified by active mods.
    /// </summary>
    List<VanillaItem> GetAvailableReplacements(string slotKey, EffectiveCollectionState globalState);
}