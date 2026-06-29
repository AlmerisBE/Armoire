namespace Armoire.Features.VanillaSearch.UI;

using Armoire.Core.Localization;
using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.ModSwapper;
using Armoire.Features.VanillaSearch.Models;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class VanillaReplacementWindow {
    private readonly IVanillaSearchService searchService;
    private readonly ITextureProvider textureProvider;
    private readonly ILocalizationService loc;
    private readonly IModSwapperService swapperService;

    public bool IsVisible { get; set; } = false;
    private string currentSlotKey = string.Empty;
    private string currentModId = string.Empty;

    // 3 distinct search inputs
    private string searchName = string.Empty;
    private string searchExpansion = string.Empty;
    private string searchOrigin = string.Empty;

    private List<VanillaItem> availableItems = new();
    private EffectiveCollectionState? lastGlobalState;

    public VanillaReplacementWindow(IVanillaSearchService searchService, ITextureProvider textureProvider, ILocalizationService loc, IModSwapperService swapperService) {
        this.searchService = searchService;
        this.textureProvider = textureProvider;
        this.loc = loc;
        this.swapperService = swapperService;
    }

    public void Open(string slotKey, string modId, EffectiveCollectionState globalState) {
        this.currentSlotKey = slotKey;
        this.currentModId = modId;
        this.lastGlobalState = globalState;

        // Reset all filters when opening the window
        this.searchName = string.Empty;
        this.searchExpansion = string.Empty;
        this.searchOrigin = string.Empty;

        // Fetch eligible items immediately on open
        this.availableItems = this.searchService.GetAvailableReplacements(slotKey, globalState);
        this.IsVisible = true;
    }

    public void Draw() {
        if (!this.IsVisible) {
            return;
        }

        bool windowOpen = this.IsVisible;
        ImGui.SetNextWindowSize(new Vector2(650, 550), ImGuiCond.FirstUseEver);

        if (ImGui.Begin(this.loc.GetString("UI_VanillaWindow_Title"), ref windowOpen, ImGuiWindowFlags.NoCollapse)) {
            if (!windowOpen) {
                this.IsVisible = false;
            }

            // --- Multi-Criteria Search Filters ---

            // 1. Name Filter (Full Width)
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("##SearchName", this.loc.GetString("UI_VanillaWindow_SearchName"), ref this.searchName, 128);

            // 2. Expansion and Origin Filters (Half Width each, side-by-side)
            float halfWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2f;

            ImGui.SetNextItemWidth(halfWidth);
            ImGui.InputTextWithHint("##SearchExpansion", this.loc.GetString("UI_VanillaWindow_SearchExp"), ref this.searchExpansion, 128);

            ImGui.SameLine();

            ImGui.SetNextItemWidth(halfWidth);
            ImGui.InputTextWithHint("##SearchOrigin", this.loc.GetString("UI_VanillaWindow_SearchOrigin"), ref this.searchOrigin, 128);

            ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

            // --- Apply Filters (AND logic) ---
            var filteredItems = this.availableItems.Where(i =>
                (string.IsNullOrWhiteSpace(this.searchName) || i.Name.Contains(this.searchName, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(this.searchExpansion) || i.ExpansionName.Contains(this.searchExpansion, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(this.searchOrigin) || i.Origin.Contains(this.searchOrigin, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            // --- Draw List ---
            if (ImGui.BeginChild("VanillaItemsList", new Vector2(0, 0), true)) {
                if (!filteredItems.Any()) {
                    ImGui.Spacing();
                    ImGui.TextDisabled(this.loc.GetString("UI_VanillaWindow_NoResults"));
                }

                foreach (var item in filteredItems) {
                    ImGui.PushID($"vanilla_{item.ItemId}");

                    // 1. Draw Item Icon
                    if (item.IconId > 0) {
                        var iconWrap = this.textureProvider.GetFromGameIcon(new GameIconLookup(item.IconId)).GetWrapOrDefault();
                        if (iconWrap != null) {
                            ImGui.Image(iconWrap.Handle, new Vector2(40, 40));
                            ImGui.SameLine();
                        }
                    }

                    // 2. Draw Metadata Block
                    Vector2 textPos = ImGui.GetCursorPos();
                    ImGui.SetCursorPos(new Vector2(textPos.X, textPos.Y + 2));
                    ImGui.TextUnformatted(item.Name);

                    ImGui.SetCursorPos(new Vector2(textPos.X, textPos.Y + 18));
                    ImGui.TextDisabled($"{item.ExpansionName} | {item.Origin}");

                    // 3. Draw Selection Button aligned to the right side
                    ImGui.SameLine(ImGui.GetWindowWidth() - 90);
                    ImGui.SetCursorPosY(textPos.Y + 6);
                    if (ImGui.Button(this.loc.GetString("UI_VanillaWindow_BtnChoose"))) {
                        bool success = this.swapperService.PerformSwap(this.currentModId, this.currentSlotKey, item.ModelId);
                        this.IsVisible = false;
                    }

                    ImGui.PopID();
                    ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
                }
                ImGui.EndChild();
            }
        }
        ImGui.End();
    }
}