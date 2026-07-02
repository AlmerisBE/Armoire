namespace Armoire.Features.Outfits.UI;

using Armoire.Core.Localization;
using Armoire.Features.Outfits.Presentation;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class OutfitDetailsWindow {
    private readonly ILocalizationService loc;
    private readonly ITextureProvider textureProvider;
    private readonly IOutfitDetailsPresenter presenter;
    private readonly IOutfitsPresenter outfitsPresenter;

    private bool isEditing = false;

    public OutfitDetailsWindow(
        ILocalizationService loc,
        ITextureProvider textureProvider,
        IOutfitDetailsPresenter presenter,
        IOutfitsPresenter outfitsPresenter) {

        this.loc = loc;
        this.textureProvider = textureProvider;
        this.presenter = presenter;
        this.outfitsPresenter = outfitsPresenter;
    }

    public void Draw() {
        var currentOutfit = this.presenter.CurrentOutfit;

        if (!this.presenter.IsVisible || currentOutfit == null) {
            return;
        }

        bool windowOpen = this.presenter.IsVisible;
        ImGui.SetNextWindowSize(new Vector2(850, 600), ImGuiCond.FirstUseEver);

        string title = string.Format(this.loc.GetString("OutfitDetails_WindowTitle"), currentOutfit.Name) + "###OutfitDetailsWindow";

        if (ImGui.Begin(title, ref windowOpen)) {
            if (!windowOpen) {
                this.presenter.IsVisible = false;
                this.isEditing = false;
            }

            ImGui.TextDisabled(string.Format(this.loc.GetString("OutfitDetails_SavedOn"), currentOutfit.CreatedAt.LocalDateTime.ToString("dd/MM/yyyy HH:mm")));

            ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - (this.isEditing ? 120f : 370f));

            if (this.isEditing) {
                if (ImGui.Button(this.loc.GetString("OutfitDetails_BtnDone"), new Vector2(100, 0))) {
                    this.isEditing = false;
                    this.outfitsPresenter.UpdateOutfit(currentOutfit);
                }
            } else {
                if (ImGui.Button(this.loc.GetString("OutfitDetails_BtnEdit"))) {
                    this.isEditing = true;
                }
                ImGui.SameLine();

                if (ImGui.Button(this.loc.GetString("OutfitDetails_BtnActivate"))) {
                    this.outfitsPresenter.ActivateOutfit(currentOutfit);
                }
                ImGui.SameLine();

                if (ImGui.Button(this.loc.GetString("OutfitDetails_BtnShare"))) {
                    this.outfitsPresenter.ExportToClipboard(currentOutfit);
                }
                ImGui.SameLine();

                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1.0f, 0.3f, 0.3f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.1f, 0.1f, 1.0f));
                if (ImGui.Button(this.loc.GetString("OutfitDetails_BtnDelete"))) {
                    this.outfitsPresenter.DeleteOutfit(currentOutfit.Id);
                    this.presenter.IsVisible = false;
                }
                ImGui.PopStyleColor(3);
            }

            ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

            // --- TABLE 1: EQUIPMENT ---
            int equipCols = this.isEditing ? 5 : 4;
            if (ImGui.BeginTable("OutfitDetailsEquipTable", equipCols, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, 250))) {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColSlot"), ImGuiTableColumnFlags.WidthFixed, 130f);
                ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColItem"), ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColStats"), ImGuiTableColumnFlags.WidthFixed, 140f);
                ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColWinner"), ImGuiTableColumnFlags.WidthStretch);
                if (this.isEditing) {
                    ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColAction"), ImGuiTableColumnFlags.WidthFixed, 30f);
                }

                ImGui.TableHeadersRow();

                var pieces = currentOutfit.Equipment ?? new();

                // Retrieve the list of ignored mod names to clean up the equipment table dynamically
                var ignoredModNames = currentOutfit.RequiredMods?
                    .Where(m => m.IsIgnored)
                    .Select(m => m.Name)
                    .ToHashSet(System.StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

                int ringCount = 0;

                foreach (var piece in pieces) {
                    bool wasIgnored = piece.IsIgnored;
                    // If the equipment is ignored, make the entire row semi-transparent
                    if (wasIgnored) {
                        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.4f);
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();

                    string slotName = GetFriendlySlotName(piece.Category);
                    if (piece.Category == "rir" || piece.Category == "ril") {
                        ringCount++;
                        slotName = ringCount == 1 ? this.loc.GetString("Slot_RingRight") : this.loc.GetString("Slot_RingLeft");
                    }
                    ImGui.TextUnformatted(slotName);

                    ImGui.TableNextColumn();
                    if (piece.IconId > 0) {
                        var iconWrap = this.textureProvider.GetFromGameIcon(new GameIconLookup(piece.IconId)).GetWrapOrDefault();
                        if (iconWrap != null) { ImGui.Image(iconWrap.Handle, new Vector2(24, 24)); ImGui.SameLine(); }
                    }
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextUnformatted(piece.Name);

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextDisabled($"(Niv. {piece.EquipLevel} - iLvl {piece.ItemLevel})");

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();

                    // DYNAMIC CLEANUP: Remove mods that were explicitly ignored in the bottom table
                    string vanillaStr = this.loc.GetString("Outfits_VanillaMod");
                    var activeMods = piece.ModifyingModNames
                        .Split(new[] { ", " }, System.StringSplitOptions.RemoveEmptyEntries)
                        .Where(m => m != vanillaStr && !ignoredModNames.Contains(m))
                        .ToList();

                    string displayMods = activeMods.Count > 0 ? string.Join(", ", activeMods) : vanillaStr;

                    if (displayMods == vanillaStr) {
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
                        ImGui.TextWrapped(displayMods);
                        ImGui.PopStyleColor();
                    } else {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 0.8f, 1.0f, 1.0f));
                        ImGui.TextWrapped(displayMods);
                        ImGui.PopStyleColor();
                        if (ImGui.IsItemHovered()) {
                            ImGui.SetTooltip(displayMods);
                        }
                    }

                    if (this.isEditing) {
                        ImGui.TableNextColumn();
                        string btnIcon = piece.IsIgnored ? FontAwesomeIcon.EyeSlash.ToIconString() : FontAwesomeIcon.Eye.ToIconString();

                        ImGui.PushFont(UiBuilder.IconFont);
                        if (ImGui.Button($"{btnIcon}##eq_{piece.ItemId}")) {
                            piece.IsIgnored = !piece.IsIgnored; // Toggle the state
                        }
                        ImGui.PopFont();
                    }

                    if (wasIgnored) {
                        ImGui.PopStyleVar();
                    }
                }
                ImGui.EndTable();
            }

            ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

            // --- TABLE 2: REQUIRED MODS ---
            ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), this.loc.GetString("OutfitDetails_ModsTitle"));
            ImGui.Spacing();

            int modCols = this.isEditing ? 3 : 2;
            if (ImGui.BeginTable("OutfitDetailsModsTable", modCols, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY)) {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColModName"), ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColInternalId"), ImGuiTableColumnFlags.WidthStretch);
                if (this.isEditing) {
                    ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColAction"), ImGuiTableColumnFlags.WidthFixed, 30f);
                }

                ImGui.TableHeadersRow();

                var reqs = currentOutfit.RequiredMods ?? new();

                foreach (var req in reqs) {
                    bool wasIgnored = req.IsIgnored;

                    if (wasIgnored) {
                        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.4f);
                    }

                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextUnformatted(req.Name);

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextDisabled(req.ModId);

                    if (this.isEditing) {
                        ImGui.TableNextColumn();
                        string btnIcon = req.IsIgnored ? FontAwesomeIcon.EyeSlash.ToIconString() : FontAwesomeIcon.Eye.ToIconString();

                        ImGui.PushFont(UiBuilder.IconFont);
                        if (ImGui.Button($"{btnIcon}##mod_{req.ModId}")) {
                            req.IsIgnored = !req.IsIgnored; // Toggle the state
                        }
                        ImGui.PopFont();
                    }

                    if (wasIgnored) {
                        ImGui.PopStyleVar();
                    }
                }
                ImGui.EndTable();
            }
        }
        ImGui.End();
    }

    private string GetFriendlySlotName(string slotKey) {
        return slotKey switch {
            "wpn" => this.loc.GetString("Slot_MainHand"),
            "sub" => this.loc.GetString("Slot_OffHand"),
            "met" => this.loc.GetString("Slot_Head"),
            "top" => this.loc.GetString("Slot_Body"),
            "glv" => this.loc.GetString("Slot_Hands"),
            "dwn" => this.loc.GetString("Slot_Legs"),
            "sho" => this.loc.GetString("Slot_Feet"),
            "ear" => this.loc.GetString("Slot_Earrings"),
            "nek" => this.loc.GetString("Slot_Necklace"),
            "wrs" => this.loc.GetString("Slot_Bracelets"),
            "rir" => this.loc.GetString("Slot_RingRight"),
            "ril" => this.loc.GetString("Slot_RingLeft"),
            "custom" => this.loc.GetString("Slot_Custom"),
            _ => slotKey
        };
    }
}