namespace Armoire.Features.Outfits.UI;

using Armoire.Core.Localization;
using Armoire.Features.Outfits.Presentation;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using System.Linq;
using System.Numerics;

public class OutfitImportWindow {
    private readonly ILocalizationService loc;
    private readonly ITextureProvider textureProvider;
    private readonly IOutfitImportPresenter presenter;

    public OutfitImportWindow(ILocalizationService loc, ITextureProvider textureProvider, IOutfitImportPresenter presenter) {
        this.loc = loc;
        this.textureProvider = textureProvider;
        this.presenter = presenter;
    }

    public void Draw() {
        if (!this.presenter.IsVisible) {
            return;
        }

        bool windowOpen = this.presenter.IsVisible;
        ImGui.SetNextWindowSize(new Vector2(850, 650), ImGuiCond.FirstUseEver);

        if (ImGui.Begin(this.loc.GetString("Import_WindowTitle"), ref windowOpen)) {
            if (!windowOpen) {
                this.presenter.CancelImport();
            }

            // --- INPUT SECTION ---
            string code = this.presenter.ShareCode;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 220f);
            if (ImGui.InputTextWithHint("##ImportCode", this.loc.GetString("Import_InputHint"), ref code, 16384)) {
                this.presenter.ShareCode = code;
            }
            ImGui.SameLine();
            if (ImGui.Button(this.loc.GetString("Import_BtnAnalyze"), new Vector2(200, 0))) {
                this.presenter.AnalyzeCode();
            }

            if (!string.IsNullOrEmpty(this.presenter.ErrorMessage)) {
                ImGui.Spacing();
                ImGui.TextColored(new Vector4(1.0f, 0.2f, 0.2f, 1.0f), this.loc.GetString(this.presenter.ErrorMessage));
            }

            ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

            // --- STAGED OUTFIT PREVIEW ---
            var outfit = this.presenter.StagedOutfit;
            if (outfit != null) {
                // Header: Missing Mods Warning
                if (this.presenter.MissingMods.Count > 0) {
                    ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.2f, 0.05f, 0.05f, 1.0f));
                    if (ImGui.BeginChild("MissingModsWarning", new Vector2(0, 100), true)) {
                        ImGui.TextColored(new Vector4(1.0f, 0.4f, 0.4f, 1.0f), this.loc.GetString("Import_MissingModsHeader"));
                        ImGui.TextWrapped(this.loc.GetString("Import_MissingModsDesc"));
                        ImGui.Spacing();
                        foreach (var missing in this.presenter.MissingMods) {
                            ImGui.TextUnformatted($"- {missing.Name} (Auteur: {missing.Author})");
                        }
                    }
                    ImGui.EndChild();
                    ImGui.PopStyleColor();
                    ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
                }

                // Table 1: Equipment
                ImGui.SetWindowFontScale(1.2f);
                ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), outfit.Name);
                ImGui.SetWindowFontScale(1.0f);
                ImGui.Spacing();

                if (ImGui.BeginTable("ImportEquipTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, 200))) {
                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColSlot"), ImGuiTableColumnFlags.WidthFixed, 130f);
                    ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColItem"), ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColStats"), ImGuiTableColumnFlags.WidthFixed, 140f);
                    ImGui.TableSetupColumn(this.loc.GetString("OutfitDetails_ColWinner"), ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();

                    var pieces = outfit.Equipment ?? new();
                    foreach (var piece in pieces) {
                        if (piece.IsIgnored) {
                            continue; // Don't show ignored pieces in the import preview
                        }

                        // Check if this specific piece relies on a missing mod
                        bool isImpacted = this.presenter.MissingMods.Any(m => piece.ModifyingModNames.Contains(m.Name));

                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(GetFriendlySlotName(piece.Category));

                        ImGui.TableNextColumn();
                        if (piece.IconId > 0) {
                            var iconWrap = this.textureProvider.GetFromGameIcon(new GameIconLookup(piece.IconId)).GetWrapOrDefault();
                            if (iconWrap != null) { ImGui.Image(iconWrap.Handle, new Vector2(24, 24)); ImGui.SameLine(); }
                        }

                        if (isImpacted) {
                            ImGui.TextColored(new Vector4(1.0f, 0.4f, 0.4f, 1.0f), piece.Name);
                        } else {
                            ImGui.TextUnformatted(piece.Name);
                        }

                        ImGui.TableNextColumn();
                        ImGui.TextDisabled($"(Niv. {piece.EquipLevel} - iLvl {piece.ItemLevel})");

                        ImGui.TableNextColumn();
                        if (isImpacted) {
                            ImGui.TextColored(new Vector4(1.0f, 0.4f, 0.4f, 1.0f), $"{piece.ModifyingModNames} [{this.loc.GetString("Import_StatusMissing")}]");
                        } else {
                            ImGui.TextWrapped(piece.ModifyingModNames);
                        }
                    }
                    ImGui.EndTable();
                }

                ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

                // Footer: Actions
                if (ImGui.Button(this.loc.GetString("Import_BtnConfirm"), new Vector2(200, 30))) {
                    this.presenter.ConfirmImport();
                }
                ImGui.SameLine();
                if (ImGui.Button(this.loc.GetString("Import_BtnCancel"), new Vector2(120, 30))) {
                    this.presenter.CancelImport();
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