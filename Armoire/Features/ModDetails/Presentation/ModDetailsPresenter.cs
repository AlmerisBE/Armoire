namespace Armoire.Features.ModDetails.Presentation;

using Armoire.Features.ConflictEngine;
using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.GlamourerIpc;
using Armoire.Features.ModDetails;
using Armoire.Features.ModDetails.Models;
using Armoire.Features.ModSwapper;
using Armoire.Features.VanillaSearch.UI;
using System.Collections.Generic;
using System.Linq;

public class ModDetailsPresenter : IModDetailsPresenter {
    private readonly IModDetailsResolver resolver;
    private readonly IPenumbraSyncManager syncManager;
    private readonly IModSwapperService swapperService;
    private readonly IGlamourerClient glamourerClient;
    private readonly VanillaReplacementWindow vanillaWindow;

    public bool IsVisible { get; set; } = false;
    public DetailedModState? CurrentState { get; private set; }

    private string currentModId = string.Empty;
    private EffectiveCollectionState? currentGlobalState;

    // UI session cache for texture inheritance selection
    private readonly Dictionary<string, string> selectedTextureProviders = new();

    public ModDetailsPresenter(
        IModDetailsResolver resolver,
        IPenumbraSyncManager syncManager,
        IModSwapperService swapperService,
        IGlamourerClient glamourerClient,
        VanillaReplacementWindow vanillaWindow) {
        this.resolver = resolver;
        this.syncManager = syncManager;
        this.swapperService = swapperService;
        this.glamourerClient = glamourerClient;
        this.vanillaWindow = vanillaWindow;

        this.syncManager.OnStatusUpdated += RefreshData;
    }

    public void Open(string modId) {
        this.currentModId = modId;

        // Clear temporary UI cache for this specific mod to ensure sync with config
        var keysToRemove = this.selectedTextureProviders.Keys
            .Where(k => k.StartsWith($"{modId}_"))
            .ToList();
        foreach (var key in keysToRemove) {
            this.selectedTextureProviders.Remove(key);
        }

        this.syncManager.ForceRefresh();
        this.IsVisible = true;
    }

    public void UpdateState(EffectiveCollectionState? globalState) {
        this.currentGlobalState = globalState;

        // Resolve mod details if the window is open and state is available
        if (this.IsVisible && !string.IsNullOrEmpty(this.currentModId) && globalState != null) {
            this.CurrentState = this.resolver.ResolveModDetails(this.currentModId, globalState);
        }
    }

    public void ResetCurrentMod() {
        if (this.CurrentState != null) {
            string targetId = this.CurrentState.ModId;
            this.swapperService.ResetMod(targetId);

            // Clear temporary UI cache for this specific mod upon reset
            var keysToRemove = this.selectedTextureProviders.Keys
                .Where(k => k.StartsWith($"{targetId}_"))
                .ToList();
            foreach (var key in keysToRemove) {
                this.selectedTextureProviders.Remove(key);
            }
        }
    }

    public void EquipItem(uint itemId, string slotKey) {
        this.glamourerClient.EquipItem(itemId, slotKey);
    }

    public void OpenVanillaReplacement(string slotKey, string originalProviderId) {
        if (this.currentGlobalState != null && this.CurrentState != null) {
            string cacheKey = $"{this.CurrentState.ModId}_{slotKey}";
            if (!this.selectedTextureProviders.TryGetValue(cacheKey, out string? providerIdToPass)) {
                providerIdToPass = originalProviderId;
            }
            this.vanillaWindow.Open(slotKey, this.CurrentState.ModId, this.currentGlobalState, providerIdToPass ?? string.Empty);
        }
    }

    public string GetSelectedProvider(string slotKey, string originalProviderId) {
        if (this.CurrentState == null) {
            return originalProviderId;
        }

        string cacheKey = $"{this.CurrentState.ModId}_{slotKey}";
        if (!this.selectedTextureProviders.TryGetValue(cacheKey, out string? selectedProviderId)) {
            selectedProviderId = originalProviderId;
        }
        return selectedProviderId ?? string.Empty;
    }

    public void SetSelectedProvider(string slotKey, string providerId) {
        if (this.CurrentState == null) {
            return;
        }

        string cacheKey = $"{this.CurrentState.ModId}_{slotKey}";
        this.selectedTextureProviders[cacheKey] = providerId;
    }

    private void RefreshData(PenumbraStatusResult fullStatus) {
        if (!this.IsVisible || string.IsNullOrEmpty(this.currentModId)) {
            return;
        }
    }

    public void Dispose() {
        this.syncManager.OnStatusUpdated -= RefreshData;
    }
}