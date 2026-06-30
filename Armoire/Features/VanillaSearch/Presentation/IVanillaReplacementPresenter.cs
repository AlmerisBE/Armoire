namespace Armoire.Features.VanillaSearch.Presentation;

using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.VanillaSearch.Models;
using System.Collections.Generic;

public interface IVanillaReplacementPresenter {
    // --- STATE DATA ---
    bool IsVisible { get; set; }
    string SearchName { get; set; }
    string SearchExpansion { get; set; }
    string SearchOrigin { get; set; }

    /// <summary>
    /// Gets the list of available items dynamically filtered by the current search criteria.
    /// </summary>
    IReadOnlyList<VanillaItem> FilteredItems { get; }

    // --- ACTIONS ---
    void Open(string slotKey, string modId, EffectiveCollectionState globalState, string textureProviderId = "");
    void SelectReplacement(string targetModelId);
    void EquipItemInGame(uint itemId);
}