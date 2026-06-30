namespace Armoire.Features.VanillaSearch.Presentation;

using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.GlamourerIpc;
using Armoire.Features.ModSwapper;
using Armoire.Features.VanillaSearch;
using Armoire.Features.VanillaSearch.Models;
using System;
using System.Collections.Generic;
using System.Linq;

public class VanillaReplacementPresenter : IVanillaReplacementPresenter {
    private readonly IVanillaSearchService searchService;
    private readonly IModSwapperService swapperService;
    private readonly IGlamourerClient glamourerClient;

    public bool IsVisible { get; set; } = false;
    public string SearchName { get; set; } = string.Empty;
    public string SearchExpansion { get; set; } = string.Empty;
    public string SearchOrigin { get; set; } = string.Empty;

    private string currentSlotKey = string.Empty;
    private string currentModId = string.Empty;
    private string currentTextureProviderId = string.Empty;
    private List<VanillaItem> availableItems = new();

    public IReadOnlyList<VanillaItem> FilteredItems {
        get {
            return this.availableItems.Where(i =>
                (string.IsNullOrWhiteSpace(this.SearchName) || i.Name.Contains(this.SearchName, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(this.SearchExpansion) || i.ExpansionName.Contains(this.SearchExpansion, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(this.SearchOrigin) || i.Origin.Contains(this.SearchOrigin, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }
    }

    public VanillaReplacementPresenter(
        IVanillaSearchService searchService,
        IModSwapperService swapperService,
        IGlamourerClient glamourerClient) {
        this.searchService = searchService;
        this.swapperService = swapperService;
        this.glamourerClient = glamourerClient;
    }

    public void Open(string slotKey, string modId, EffectiveCollectionState globalState, string textureProviderId = "") {
        this.currentSlotKey = slotKey;
        this.currentModId = modId;
        this.currentTextureProviderId = textureProviderId;

        // Reset all filters when opening the window
        this.SearchName = string.Empty;
        this.SearchExpansion = string.Empty;
        this.SearchOrigin = string.Empty;

        // Fetch eligible items immediately on open
        this.availableItems = this.searchService.GetAvailableReplacements(slotKey, globalState);
        this.IsVisible = true;
    }

    public void SelectReplacement(string targetModelId) {
        // Trigger the dual-strike swap logic
        bool success = this.swapperService.PerformSwap(
            this.currentModId,
            this.currentSlotKey,
            targetModelId,
            this.currentTextureProviderId
        );

        if (success) {
            this.IsVisible = false;
        }
    }

    public void EquipItemInGame(uint itemId) {
        this.glamourerClient.EquipItem(itemId, this.currentSlotKey);
    }
}