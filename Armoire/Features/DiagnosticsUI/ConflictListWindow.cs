namespace Armoire.Features.DiagnosticsUI;

using Armoire.Core.Localization;
using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.DiagnosticsUI.Presentation;
using Dalamud.Bindings.ImGui;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class ConflictListWindow {
    private readonly ILocalizationService loc;
    private readonly IConflictListPresenter presenter;

    public ConflictListWindow(ILocalizationService localizationService, IConflictListPresenter presenter) {
        this.loc = localizationService;
        this.presenter = presenter;
    }

    public void Draw(List<PenumbraMod> liveConflicts) {
        if (!this.presenter.IsVisible) {
            return;
        }

        // Push live data to the presenter
        this.presenter.UpdateLiveConflicts(liveConflicts);

        bool windowOpen = this.presenter.IsVisible;
        ImGui.SetNextWindowSize(new Vector2(900, 500), ImGuiCond.FirstUseEver);

        if (ImGui.Begin(this.loc.GetString("Conflict_WindowTitle"), ref windowOpen)) {
            if (!windowOpen) {
                this.presenter.IsVisible = false;
            }

            ImGui.TextWrapped(string.Format(this.loc.GetString("Conflict_Description"), liveConflicts?.Count ?? 0));
            ImGui.Spacing();

            string searchQuery = this.presenter.SearchQuery;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.InputTextWithHint("##SearchConflict", this.loc.GetString("Conflict_SearchHint"), ref searchQuery, 256)) {
                this.presenter.SearchQuery = searchQuery;
            }
            ImGui.Spacing();

            DrawModifiedModsSection();
            DrawConflictsSection();
        }
        ImGui.End();
    }

    private void DrawModifiedModsSection() {
        var filteredModifiedMods = this.presenter.FilteredModifiedMods;
        if (filteredModifiedMods.Count == 0) {
            return;
        }

        ImGui.TextColored(new Vector4(0.2f, 1.0f, 0.2f, 1.0f), this.loc.GetString("Conflict_ModifiedModsTitle"));
        ImGui.Separator();

        if (ImGui.BeginTable("ModifiedModsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg)) {
            ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColModName"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColState"), ImGuiTableColumnFlags.WidthFixed, 150f);
            ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColAction"), ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableHeadersRow();

            foreach (var kvp in filteredModifiedMods) {
                string modId = kvp.Key;
                var entry = kvp.Value;

                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                if (ImGui.Selectable($"{entry.ModName}##{modId}", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap)) {
                    this.presenter.OpenModDetails(modId);
                }
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip(string.Format(this.loc.GetString("Conflict_TooltipSwaps"), modId, entry.Swaps.Count));
                }

                ImGui.TableNextColumn();
                bool isPatched = this.presenter.IsModActivelyPatched(modId);

                if (isPatched) {
                    ImGui.TextColored(new Vector4(0.2f, 1.0f, 0.2f, 1.0f), this.loc.GetString("Conflict_StateActive"));
                } else {
                    ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.0f, 1.0f), this.loc.GetString("Conflict_StateUpdate"));
                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip(this.loc.GetString("Conflict_TooltipUpdate"));
                    }
                }

                ImGui.TableNextColumn();
                if (isPatched) {
                    if (ImGui.Button($"{this.loc.GetString("Conflict_BtnReset")}##{modId}")) {
                        this.presenter.ResetMod(modId);
                    }
                } else {
                    if (ImGui.Button($"{this.loc.GetString("Conflict_BtnRestore")}##{modId}")) {
                        this.presenter.RestoreMod(modId, entry.Swaps);
                    }
                }
            }
            ImGui.EndTable();
        }
        ImGui.Spacing(); ImGui.Spacing();
    }

    private void DrawConflictsSection() {
        ImGui.TextColored(new Vector4(1.0f, 0.2f, 0.2f, 1.0f), this.loc.GetString("Conflict_Title_List"));
        ImGui.Separator();

        var filteredConflicts = this.presenter.FilteredConflicts;

        if (ImGui.BeginTable("ConflictsTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable)) {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColVictim"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColSlots"), ImGuiTableColumnFlags.WidthFixed, 150f);
            ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColWinners"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColPriority"), ImGuiTableColumnFlags.WidthFixed, 60f);
            ImGui.TableHeadersRow();

            foreach (var mod in filteredConflicts) {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                if (ImGui.Selectable($"{mod.Name}##{mod.Id}", false, ImGuiSelectableFlags.SpanAllColumns)) {
                    this.presenter.OpenModDetails(mod.Id);
                }

                ImGui.TableNextColumn();
                ImGui.TextWrapped(string.Join(", ", mod.ConflictingSlots));

                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(1.0f, 0.4f, 0.0f, 1.0f), string.Join(", ", mod.OverwrittenBy));

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(mod.Priority.ToString());
            }
            ImGui.EndTable();
        }
    }
}