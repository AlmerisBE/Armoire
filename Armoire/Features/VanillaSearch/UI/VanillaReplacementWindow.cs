namespace Armoire.Features.VanillaSearch.UI;

using Armoire.Core.Localization;
using Armoire.Features.VanillaSearch.Presentation;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using System.Linq;
using System.Numerics;

public class VanillaReplacementWindow {
    private readonly ILocalizationService loc;
    private readonly ITextureProvider textureProvider;
    private readonly IVanillaReplacementPresenter presenter;

    public VanillaReplacementWindow(
        ILocalizationService loc,
        ITextureProvider textureProvider,
        IVanillaReplacementPresenter presenter) {
        this.loc = loc;
        this.textureProvider = textureProvider;
        this.presenter = presenter;
    }

    public void Draw() {
        if (!this.presenter.IsVisible) {
            return;
        }

        bool windowOpen = this.presenter.IsVisible;
        ImGui.SetNextWindowSize(new Vector2(650, 550), ImGuiCond.FirstUseEver);

        if (ImGui.Begin(this.loc.GetString("UI_VanillaWindow_Title"), ref windowOpen, ImGuiWindowFlags.NoCollapse)) {
            if (!windowOpen) {
                this.presenter.IsVisible = false;
            }

            // Local variables for ImGui ref inputs
            string searchName = this.presenter.SearchName;
            string searchExpansion = this.presenter.SearchExpansion;
            string searchOrigin = this.presenter.SearchOrigin;

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.InputTextWithHint("##SearchName", this.loc.GetString("UI_VanillaWindow_SearchName"), ref searchName, 128)) {
                this.presenter.SearchName = searchName;
            }

            float halfWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2f;

            ImGui.SetNextItemWidth(halfWidth);
            if (ImGui.InputTextWithHint("##SearchExpansion", this.loc.GetString("UI_VanillaWindow_SearchExp"), ref searchExpansion, 128)) {
                this.presenter.SearchExpansion = searchExpansion;
            }

            ImGui.SameLine();

            ImGui.SetNextItemWidth(halfWidth);
            if (ImGui.InputTextWithHint("##SearchOrigin", this.loc.GetString("UI_VanillaWindow_SearchOrigin"), ref searchOrigin, 128)) {
                this.presenter.SearchOrigin = searchOrigin;
            }

            ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

            var filteredItems = this.presenter.FilteredItems;

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

                    if (item.EquipLevel > 0 || item.ItemLevel > 0) {
                        ImGui.SameLine();
                        ImGui.SetCursorPosY(textPos.Y + 2);
                        ImGui.TextDisabled($"(Niv. {item.EquipLevel} - iLvl {item.ItemLevel})");
                    }

                    ImGui.SetCursorPos(new Vector2(textPos.X, textPos.Y + 18));
                    ImGui.TextDisabled($"{item.ExpansionName} | {item.Origin}");

                    ImGui.SameLine(ImGui.GetWindowWidth() - 90);
                    ImGui.SetCursorPosY(textPos.Y + 6);

                    // Glamourer Context Menu
                    if (ImGui.BeginPopupContextItem($"vanilla_ctx_{item.ItemId}")) {
                        if (ImGui.Selectable(this.loc.GetString("UI_ContextMenu_EquipGlamourer"))) {
                            this.presenter.EquipItemInGame(item.ItemId);
                        }
                        ImGui.EndPopup();
                    }
                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip("Clic-droit pour plus d'options");
                    }

                    if (ImGui.Button(this.loc.GetString("UI_VanillaWindow_BtnChoose"))) {
                        this.presenter.SelectReplacement(item.ModelId);
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