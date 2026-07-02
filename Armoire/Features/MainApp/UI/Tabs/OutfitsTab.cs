namespace Armoire.Features.MainApp.UI.Tabs;

using Armoire.Core.Localization;
using Armoire.Features.Outfits.Models;
using Armoire.Features.Outfits.Presentation;
using Dalamud.Bindings.ImGui;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class OutfitsTab {
    private readonly ILocalizationService loc;
    private readonly IOutfitsPresenter presenter;
    private readonly IOutfitDetailsPresenter detailsPresenter;
    private string newOutfitName = string.Empty;

    public OutfitsTab(ILocalizationService loc, IOutfitsPresenter presenter, IOutfitDetailsPresenter detailsPresenter) {
        this.loc = loc;
        this.presenter = presenter;
        this.detailsPresenter = detailsPresenter;
    }

    public void Draw() {
        ImGui.Spacing();

        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), this.loc.GetString("Outfits_CreateTitle"));
        ImGui.SetNextItemWidth(300f);
        ImGui.InputTextWithHint("##NewOutfitName", this.loc.GetString("Outfits_NameHint"), ref this.newOutfitName, 64);
        ImGui.SameLine();

        if (ImGui.Button(this.loc.GetString("Outfits_CaptureBtn"))) {
            if (!string.IsNullOrWhiteSpace(this.newOutfitName)) {
                this.presenter.CreateOutfit(this.newOutfitName);
                this.newOutfitName = string.Empty;
            }
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), this.loc.GetString("Outfits_ListTitle"));
        ImGui.Spacing();

        if (this.presenter.Outfits.Count == 0) {
            ImGui.TextDisabled(this.loc.GetString("Outfits_EmptyList"));
            return;
        }

        if (ImGui.BeginTable("OutfitsLibraryTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, 250))) {
            try {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn(this.loc.GetString("Outfits_ColName"), ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(this.loc.GetString("Outfits_ColMods"), ImGuiTableColumnFlags.WidthFixed, 150f);
                ImGui.TableSetupColumn(this.loc.GetString("Outfits_ColActions"), ImGuiTableColumnFlags.WidthFixed, 250f);
                ImGui.TableHeadersRow();

                foreach (var outfit in this.presenter.Outfits.ToList()) {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    if (ImGui.Selectable($"{outfit.Name}##select_{outfit.Id}", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap)) {
                        this.detailsPresenter.Open(outfit);
                    }
                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip(this.loc.GetString("Outfits_TooltipInspect"));
                    }

                    ImGui.TableNextColumn();
                    var readiness = this.presenter.CheckOutfitReadiness(outfit);

                    if (outfit.RequiredMods == null || outfit.RequiredMods.Count == 0) {
                        ImGui.TextDisabled(this.loc.GetString("Outfits_StatusNoMods"));
                    } else if (readiness.IsReady) {
                        ImGui.TextColored(new Vector4(0.2f, 1.0f, 0.2f, 1.0f), string.Format(this.loc.GetString("Outfits_StatusReady"), outfit.RequiredMods.Count));
                    } else {
                        ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.0f, 1.0f), string.Format(this.loc.GetString("Outfits_StatusMissing"), readiness.MissingModNames.Count));

                        if (ImGui.IsItemHovered()) {
                            ImGui.SetTooltip(this.loc.GetString("Outfits_TooltipMissing") + string.Join("\n", readiness.MissingModNames.Select(m => $"- {m}")));
                        }
                    }

                    ImGui.TableNextColumn();
                    if (ImGui.Button($"{this.loc.GetString("Outfits_BtnActivate")}##{outfit.Id}")) {
                        this.presenter.ActivateOutfit(outfit);
                    }
                    ImGui.SameLine();

                    if (ImGui.Button($"{this.loc.GetString("Outfits_BtnShare")}##{outfit.Id}")) {
                        this.presenter.ExportToClipboard(outfit);
                    }
                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip(this.loc.GetString("Outfits_TooltipShare"));
                    }
                    ImGui.SameLine();

                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1.0f, 0.3f, 0.3f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.1f, 0.1f, 1.0f));
                    if (ImGui.Button($"{this.loc.GetString("Outfits_BtnDelete")}##{outfit.Id}")) {
                        this.presenter.DeleteOutfit(outfit.Id);
                    }
                    ImGui.PopStyleColor(3);
                }
            } finally {
                ImGui.EndTable();
            }
        }
    }

    private void DrawRequirementAnalysisTable(IReadOnlyList<ModRequirementAnalysis> analysisResult) {
        if (ImGui.BeginTable("ImportAnalysisTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg)) {
            ImGui.TableSetupColumn("Nom du Mod", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Auteur (Aide à la recherche)", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Statut", ImGuiTableColumnFlags.WidthFixed, 200f);
            ImGui.TableHeadersRow();

            foreach (var item in analysisResult) {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(item.Requirement.Name);

                ImGui.TableNextColumn();
                ImGui.TextDisabled(string.IsNullOrEmpty(item.Requirement.Author) ? "Inconnu" : item.Requirement.Author);

                ImGui.TableNextColumn();
                switch (item.Status) {
                    case ModRequirementStatus.Ready:
                        ImGui.TextColored(new Vector4(0.2f, 1.0f, 0.2f, 1.0f), $"✅ {item.DetailMessage}");
                        break;
                    case ModRequirementStatus.DisabledOrConflicting:
                        ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.0f, 1.0f), $"⚠️ {item.DetailMessage}");
                        break;
                    case ModRequirementStatus.Missing:
                        ImGui.TextColored(new Vector4(1.0f, 0.2f, 0.2f, 1.0f), $"❌ {item.DetailMessage}");
                        break;
                }
            }
            ImGui.EndTable();
        }
    }
}