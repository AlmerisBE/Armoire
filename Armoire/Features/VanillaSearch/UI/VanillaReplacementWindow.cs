namespace Armoire.Features.VanillaSearch.UI;

using Armoire.Core.Localization;
using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.GlamourerIpc;
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
    private readonly IGlamourerClient glamourerClient;

    public bool IsVisible { get; set; } = false;
    private string currentSlotKey = string.Empty;
    private string currentModId = string.Empty;
    private string currentTextureProviderId = string.Empty; // <-- NOUVEAU

    // 3 distinct search inputs
    private string searchName = string.Empty;
    private string searchExpansion = string.Empty;
    private string searchOrigin = string.Empty;

    private List<VanillaItem> availableItems = new();
    private EffectiveCollectionState? lastGlobalState;

    public VanillaReplacementWindow(IVanillaSearchService searchService, ITextureProvider textureProvider, ILocalizationService loc, IModSwapperService swapperService, IGlamourerClient glamourerClient) {
        this.searchService = searchService;
        this.textureProvider = textureProvider;
        this.loc = loc;
        this.swapperService = swapperService;
        this.glamourerClient = glamourerClient;
    }

    // <-- NOUVEAU PARAMÈTRE OPTIONNEL
    public void Open(string slotKey, string modId, EffectiveCollectionState globalState, string textureProviderId = "") {
        this.currentSlotKey = slotKey;
        this.currentModId = modId;
        this.currentTextureProviderId = textureProviderId; // Sauvegarde du choix
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

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("##SearchName", this.loc.GetString("UI_VanillaWindow_SearchName"), ref this.searchName, 128);

            float halfWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2f;

            ImGui.SetNextItemWidth(halfWidth);
            ImGui.InputTextWithHint("##SearchExpansion", this.loc.GetString("UI_VanillaWindow_SearchExp"), ref this.searchExpansion, 128);

            ImGui.SameLine();

            ImGui.SetNextItemWidth(halfWidth);
            ImGui.InputTextWithHint("##SearchOrigin", this.loc.GetString("UI_VanillaWindow_SearchOrigin"), ref this.searchOrigin, 128);

            ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

            var filteredItems = this.availableItems.Where(i =>
                (string.IsNullOrWhiteSpace(this.searchName) || i.Name.Contains(this.searchName, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(this.searchExpansion) || i.ExpansionName.Contains(this.searchExpansion, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(this.searchOrigin) || i.Origin.Contains(this.searchOrigin, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            if (ImGui.BeginChild("VanillaItemsList", new Vector2(0, 0), true)) {
                if (!filteredItems.Any()) {
                    ImGui.Spacing();
                    ImGui.TextDisabled(this.loc.GetString("UI_VanillaWindow_NoResults"));
                }

                foreach (var item in filteredItems) {
                    ImGui.PushID($"vanilla_{item.ItemId}");

                    if (item.IconId > 0) {
                        var iconWrap = this.textureProvider.GetFromGameIcon(new GameIconLookup(item.IconId)).GetWrapOrDefault();
                        if (iconWrap != null) {
                            ImGui.Image(iconWrap.Handle, new Vector2(40, 40));
                            ImGui.SameLine();
                        }
                    }

                    Vector2 textPos = ImGui.GetCursorPos();
                    ImGui.SetCursorPos(new Vector2(textPos.X, textPos.Y + 2));
                    ImGui.TextUnformatted(item.Name);

                    ImGui.SetCursorPos(new Vector2(textPos.X, textPos.Y + 18));
                    ImGui.TextDisabled($"{item.ExpansionName} | {item.Origin}");

                    ImGui.SameLine(ImGui.GetWindowWidth() - 90);
                    ImGui.SetCursorPosY(textPos.Y + 6);

                    if (ImGui.BeginPopupContextItem($"vanilla_ctx_{item.ItemId}")) {
                        if (ImGui.Selectable(this.loc.GetString("UI_ContextMenu_EquipGlamourer"))) {
                            this.glamourerClient.EquipItem(item.ItemId, this.currentSlotKey);
                        }
                        ImGui.EndPopup();
                    }
                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip("Clic-droit pour plus d'options");
                    }

                    if (ImGui.Button(this.loc.GetString("UI_VanillaWindow_BtnChoose"))) {
                        // <-- TRANSMISSION AU SWAPPER DE LA DOUBLE FRAPPE
                        bool success = this.swapperService.PerformSwap(this.currentModId, this.currentSlotKey, item.ModelId, this.currentTextureProviderId);
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