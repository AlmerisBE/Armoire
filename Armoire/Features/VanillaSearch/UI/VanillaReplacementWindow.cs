namespace Armoire.Features.VanillaSearch.UI;

using Armoire.Core.Localization;
using Armoire.Features.ConflictEngine.Models;
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

    public bool IsVisible { get; set; } = false;
    private string currentSlotKey = string.Empty;
    private string currentModId = string.Empty;
    private string searchQuery = string.Empty;

    private List<VanillaItem> availableItems = new();
    private EffectiveCollectionState? lastGlobalState;

    public VanillaReplacementWindow(IVanillaSearchService searchService, ITextureProvider textureProvider, ILocalizationService loc) {
        this.searchService = searchService;
        this.textureProvider = textureProvider;
        this.loc = loc;
    }

    public void Open(string slotKey, string modId, EffectiveCollectionState globalState) {
        this.currentSlotKey = slotKey;
        this.currentModId = modId;
        this.lastGlobalState = globalState;
        this.searchQuery = string.Empty;

        // Fetch eligible items immediately on open
        this.availableItems = this.searchService.GetAvailableReplacements(slotKey, globalState);
        this.IsVisible = true;
    }

    public void Draw() {
        if (!this.IsVisible) {
            return;
        }

        bool windowOpen = this.IsVisible;
        ImGui.SetNextWindowSize(new Vector2(600, 500), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Sélectionner un objet de remplacement###VanillaReplacement", ref windowOpen, ImGuiWindowFlags.NoCollapse)) {
            if (!windowOpen) {
                this.IsVisible = false;
            }

            // Search Filter Bar
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("##SearchVanillaItem", "Rechercher un objet par nom...", ref this.searchQuery, 128);
            ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

            // Filter lists based on user input
            var filteredItems = string.IsNullOrWhiteSpace(this.searchQuery)
                ? this.availableItems
                : this.availableItems.Where(i => i.Name.Contains(this.searchQuery, StringComparison.OrdinalIgnoreCase)).ToList();

            if (ImGui.BeginChild("VanillaItemsList", new Vector2(0, 0), true)) {
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
                    if (ImGui.Button("Choisir")) {
                        // Future action: Trigger the ModSwapper engine with item.ModelId
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