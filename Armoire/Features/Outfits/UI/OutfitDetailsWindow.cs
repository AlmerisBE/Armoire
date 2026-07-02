namespace Armoire.Features.Outfits.UI;

using Armoire.Core.Localization;
using Armoire.Features.Outfits.Models;
using Armoire.Features.Outfits.Presentation;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
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

            int equipCols = this.isEditing ? 5 : 4;
            if (ImGui.BeginTable("OutfitDetailsEquipTable", equipCols, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, 250))) {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColSlot"), ImGuiTableColumnFlags.WidthFixed, 130f);
                ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColItem"), ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColStats"), ImGuiTableColumnFlags.WidthFixed, 140f);
                ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColWinner"), ImGuiTableColumnFlags.WidthStretch);
                if (this.isEditing) {
                    ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColAction"), ImGuiTableColumnFlags.WidthFixed, 40f);
                }

                ImGui.TableHeadersRow();

                var pieces = currentOutfit.Equipment ?? new();
                OutfitEquipmentPiece? pieceToRemove = null;
                int ringCount = 0;

                foreach (var piece in pieces) {
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
                    if (piece.ModifyingModNames.Contains(this.loc.GetString("Outfits_VanillaMod"))) {
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
                        ImGui.TextWrapped(piece.ModifyingModNames);
                        ImGui.PopStyleColor();
                    } else {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 0.8f, 1.0f, 1.0f));
                        ImGui.TextWrapped(piece.ModifyingModNames);
                        ImGui.PopStyleColor();
                    }

                    if (this.isEditing) {
                        ImGui.TableNextColumn();
                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.6f, 0.2f, 0.2f, 1.0f));
                        if (ImGui.Button($"X##eq_{piece.ItemId}")) {
                            pieceToRemove = piece;
                        }
                        ImGui.PopStyleColor();
                    }
                }
                ImGui.EndTable();

                if (pieceToRemove != null) {
                    currentOutfit.Equipment?.Remove(pieceToRemove);
                }
            }

            ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

            ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), this.loc.GetString("OutfitDetails_ModsTitle"));
            ImGui.Spacing();

            int modCols = this.isEditing ? 3 : 2;
            if (ImGui.BeginTable("OutfitDetailsModsTable", modCols, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY)) {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColModName"), ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColInternalId"), ImGuiTableColumnFlags.WidthStretch);
                if (this.isEditing) {
                    ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColAction"), ImGuiTableColumnFlags.WidthFixed, 40f);
                }

                ImGui.TableHeadersRow();

                var reqs = currentOutfit.RequiredMods ?? new();
                OutfitModRequirement? modToRemove = null;

                foreach (var req in reqs) {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextUnformatted(req.Name);

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextDisabled(req.ModId);

                    if (this.isEditing) {
                        ImGui.TableNextColumn();
                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.6f, 0.2f, 0.2f, 1.0f));
                        if (ImGui.Button($"X##mod_{req.ModId}")) {
                            modToRemove = req;
                        }
                        ImGui.PopStyleColor();
                    }
                }
                ImGui.EndTable();

                if (modToRemove != null) {
                    currentOutfit.RequiredMods?.Remove(modToRemove);
                }
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