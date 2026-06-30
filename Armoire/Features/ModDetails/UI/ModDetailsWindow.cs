namespace Armoire.Features.ModDetails.UI;

using Armoire.Core.Localization;
using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.ModDetails.Presentation;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using System.Linq;
using System.Numerics;

public class ModDetailsWindow {
    private readonly ILocalizationService loc;
    private readonly ITextureProvider textureProvider;
    private readonly IModDetailsPresenter presenter;

    public ModDetailsWindow(
        ILocalizationService loc,
        ITextureProvider textureProvider,
        IModDetailsPresenter presenter) {
        this.loc = loc;
        this.textureProvider = textureProvider;
        this.presenter = presenter;
    }

    public void Draw(EffectiveCollectionState? globalState) {
        if (!this.presenter.IsVisible) {
            return;
        }

        // Push latest state to the presenter
        this.presenter.UpdateState(globalState);

        bool windowOpen = this.presenter.IsVisible;
        ImGui.SetNextWindowSize(new Vector2(900, 650), ImGuiCond.FirstUseEver);

        var currentState = this.presenter.CurrentState;
        string modNameStr = currentState?.ModName ?? this.loc.GetString("ModDetails_Loading");
        string windowTitle = string.Format(this.loc.GetString("ModDetails_WindowTitle"), modNameStr) + "###ModDetails";

        if (ImGui.Begin(windowTitle, ref windowOpen)) {
            if (!windowOpen) {
                this.presenter.IsVisible = false;
            }

            if (currentState == null) {
                ImGui.Text(this.loc.GetString("ModDetails_Unavailable"));
                ImGui.End();
                return;
            }

            string stateStr = currentState.IsEnabled ?
                this.loc.GetString("ModDetails_StateEnabled") : this.loc.GetString("ModDetails_StateDisabled");
            ImGui.TextWrapped(string.Format(this.loc.GetString("ModDetails_Header"), currentState.Priority, stateStr));
            ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

            if (ImGui.BeginTabBar("ModDetailsTabs")) {

                if (ImGui.BeginTabItem("Résumé (Fiche d'équipement)")) {
                    DrawCharacterSheetView();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Fichiers (Vue Avancée)")) {
                    DrawAdvancedFilesView();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }
        ImGui.End();
    }

    private void DrawCharacterSheetView() {
        ImGui.Spacing();

        if (ImGui.BeginTable("CharacterSheetTable", 2, ImGuiTableFlags.None)) {
            ImGui.TableSetupColumn("LeftEquipment", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("RightEquipment", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); DrawSpecificSlot("wpn", this.loc.GetString("Slot_MainHand"));
            ImGui.TableNextColumn(); DrawSpecificSlot("sub", this.loc.GetString("Slot_OffHand"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); DrawSpecificSlot("met", this.loc.GetString("Slot_Head"));
            ImGui.TableNextColumn(); DrawSpecificSlot("ear", this.loc.GetString("Slot_Earrings"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); DrawSpecificSlot("top", this.loc.GetString("Slot_Body"));
            ImGui.TableNextColumn(); DrawSpecificSlot("nek", this.loc.GetString("Slot_Necklace"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); DrawSpecificSlot("glv", this.loc.GetString("Slot_Hands"));
            ImGui.TableNextColumn(); DrawSpecificSlot("wrs", this.loc.GetString("Slot_Bracelets"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); DrawSpecificSlot("dwn", this.loc.GetString("Slot_Legs"));
            ImGui.TableNextColumn(); DrawSpecificSlot("rir", this.loc.GetString("Slot_RingRight"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); DrawSpecificSlot("sho", this.loc.GetString("Slot_Feet"));
            ImGui.TableNextColumn(); DrawSpecificSlot("ril", this.loc.GetString("Slot_RingLeft"));

            ImGui.EndTable();
        }

        ImGui.Spacing();

        DrawSpecificSlot("custom", "Customisation");
        DrawSpecificSlot("unknown", "Fichiers Système");

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        if (ImGui.Button("Réinitialiser le mod (Paramètres par défaut)")) {
            this.presenter.ResetCurrentMod();
        }
    }

    private void DrawSpecificSlot(string slotKey, string slotDisplayName) {
        var currentState = this.presenter.CurrentState;
        if (currentState == null) {
            return;
        }

        var itemsInSlot = currentState.ReplacedSlots
            .Where(s => s.SlotCategory == slotKey)
            .GroupBy(s => new {
                s.IconId,
                BaseName = s.LocalizedItemName.Split('[')[0].Trim()
            })
            .Select(g => {
                var first = g.First();
                return new {
                    IconId = g.Key.IconId,
                    BaseName = g.Key.BaseName,
                    ItemId = first.ItemId,
                    IsConflicting = g.Any(x => x.IsConflicting),
                    OverwrittenByMods = g.SelectMany(x => x.OverwrittenByMods).Distinct().ToList(),
                    IsMissingTextures = g.Any(x => x.IsMissingTextures),
                    AvailableTextureProviders = first.AvailableTextureProviders,
                    OriginalRef = first
                };
            })
            .ToList();

        // CASE 1 : Empty slot
        if (!itemsInSlot.Any()) {
            if (slotKey == "custom" || slotKey == "unknown") {
                return;
            }

            ImGui.PushID($"empty_{slotKey}");

            Vector2 p = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddRectFilled(p, p + new Vector2(40, 40), ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 0.5f)), 4f);
            ImGui.GetWindowDrawList().AddRect(p, p + new Vector2(40, 40), ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.3f, 0.3f, 1f)), 4f);
            ImGui.Dummy(new Vector2(40, 40));

            ImGui.SameLine();
            Vector2 cursorPos = ImGui.GetCursorPos();

            ImGui.SetCursorPos(new Vector2(cursorPos.X, cursorPos.Y + 4));
            ImGui.TextDisabled(slotDisplayName);

            ImGui.SetCursorPos(new Vector2(cursorPos.X, cursorPos.Y + 20));
            ImGui.TextDisabled($"({this.loc.GetString("Slot_Unmodified")})");

            ImGui.PopID();
            ImGui.Spacing();
            return;
        }

        // CASE 2 : Modified slot
        foreach (var item in itemsInSlot) {
            ImGui.PushID(item.BaseName + item.IconId);

            ImGui.BeginGroup();
            if (item.IconId > 0) {
                var iconWrap = this.textureProvider.GetFromGameIcon(new GameIconLookup(item.IconId)).GetWrapOrDefault();
                if (iconWrap != null) {
                    ImGui.Image(iconWrap.Handle, new Vector2(40, 40));
                } else {
                    ImGui.Dummy(new Vector2(40, 40));
                }
            } else {
                ImGui.Dummy(new Vector2(40, 40));
            }

            ImGui.SameLine();
            Vector2 cursorPos = ImGui.GetCursorPos();

            if (item.IsConflicting) {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.4f, 0.4f, 1));
            }

            bool selected = ImGui.Selectable($"##select_{item.BaseName}", false, ImGuiSelectableFlags.None, new Vector2(0, 40));

            if (item.IsConflicting) {
                ImGui.PopStyleColor();
            }

            ImGui.SetCursorPos(new Vector2(cursorPos.X, cursorPos.Y + 4));
            ImGui.TextUnformatted(item.BaseName);

            if (item.IsConflicting) {
                ImGui.SetCursorPos(new Vector2(cursorPos.X, cursorPos.Y + 20));
                ImGui.TextDisabled($"(En conflit avec : {string.Join(", ", item.OverwrittenByMods)})");
            } else {
                ImGui.SetCursorPos(new Vector2(cursorPos.X, cursorPos.Y + 20));
                ImGui.TextColored(new Vector4(0.4f, 1, 0.4f, 1), "(Actif et appliqué)");
            }
            ImGui.EndGroup();

            // Glamourer Context Menu
            if (item.ItemId > 0 && ImGui.BeginPopupContextItem($"mod_ctx_{item.BaseName}_{item.IconId}")) {
                if (ImGui.Selectable(this.loc.GetString("UI_ContextMenu_EquipGlamourer"))) {
                    this.presenter.EquipItem(item.ItemId, slotKey);
                }
                ImGui.EndPopup();
            }
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Clic-droit pour plus d'options");
            }

            // Handle click to open vanilla replacement
            if (selected) {
                this.presenter.OpenVanillaReplacement(slotKey, item.OriginalRef.SelectedTextureProviderId);
            }

            // Texture inheritance UI
            if (item.IsMissingTextures) {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 48f); // Indent to align with the text
                ImGui.BeginGroup();

                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.6f, 0.0f, 1.0f));
                ImGui.TextWrapped(this.loc.GetString("ModDetails_MissingTexturesWarning"));
                ImGui.PopStyleColor();

                // Retrieve the provider state through the presenter
                string selectedProviderId = this.presenter.GetSelectedProvider(slotKey, item.OriginalRef.SelectedTextureProviderId);

                string previewValue = string.IsNullOrEmpty(selectedProviderId)
                    ? this.loc.GetString("ModDetails_SelectProvider")
                    : (item.AvailableTextureProviders.TryGetValue(selectedProviderId, out var pName) ? pName : "Unknown");

                ImGui.SetNextItemWidth(300f);
                if (ImGui.BeginCombo($"##combo_tex_{item.BaseName}", previewValue)) {
                    if (ImGui.Selectable(this.loc.GetString("ModDetails_NoProvider"), string.IsNullOrEmpty(selectedProviderId))) {
                        this.presenter.SetSelectedProvider(slotKey, string.Empty);
                    }

                    foreach (var provider in item.AvailableTextureProviders) {
                        bool isSelected = selectedProviderId == provider.Key;
                        if (ImGui.Selectable(provider.Value, isSelected)) {
                            this.presenter.SetSelectedProvider(slotKey, provider.Key);
                        }
                        if (isSelected) {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                    ImGui.EndCombo();
                }
                ImGui.EndGroup();
            }

            ImGui.PopID();
            ImGui.Spacing();
        }
    }

    private void DrawAdvancedFilesView() {
        var currentState = this.presenter.CurrentState;
        if (currentState == null) {
            return;
        }

        ImGui.Spacing();

        if (ImGui.BeginTable("ModDetailsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable)) {
            ImGui.TableSetupScrollFreeze(0, 1);

            ImGui.TableSetupColumn(this.loc.GetString("ModDetails_ColItem"), ImGuiTableColumnFlags.WidthFixed, 200f);
            ImGui.TableSetupColumn(this.loc.GetString("ModDetails_ColStatus"), ImGuiTableColumnFlags.WidthFixed, 150f);
            ImGui.TableSetupColumn(this.loc.GetString("ModDetails_ColWinner"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (var slot in currentState.ReplacedSlots) {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                string displayTitle = slot.AffectedPaths.Count > 1
                    ? $"{slot.LocalizedItemName} (x{slot.AffectedPaths.Count})"
                    : slot.LocalizedItemName;

                ImGui.TextUnformatted(displayTitle);

                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip(string.Join("\n", slot.AffectedPaths));
                }

                ImGui.TableNextColumn();

                if (slot.IsConflicting) {
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), this.loc.GetString("ModDetails_StatusConflict"));
                } else {
                    ImGui.TextColored(new Vector4(0, 1, 0, 1), this.loc.GetString("ModDetails_StatusActive"));
                }

                ImGui.TableNextColumn();

                if (slot.IsConflicting) {
                    ImGui.TextWrapped(string.Join(", ", slot.OverwrittenByMods));
                } else {
                    ImGui.TextDisabled("-");
                }
            }
            ImGui.EndTable();
        }
    }
}