namespace Armoire.Features.MainApp.UI.Tabs;

using Armoire.Core.Localization;
using Armoire.Features.MainApp.Presentation;
using Dalamud.Bindings.ImGui;
using System.Numerics;

public class ResolvedTab {
    private readonly ILocalizationService loc;
    private readonly IMainWindowPresenter presenter;

    public ResolvedTab(ILocalizationService loc, IMainWindowPresenter presenter) {
        this.loc = loc;
        this.presenter = presenter;
    }

    public void Draw() {
        ImGui.Spacing();
        var resolvedMods = this.presenter.ResolvedMods;

        if (resolvedMods.Count == 0) {
            ImGui.TextDisabled(this.loc.GetString("Main_ResolvedEmpty"));
            return;
        }

        // Search text criteria
        string query = this.presenter.ConflictSearchQuery;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.InputTextWithHint("##SearchResolved", this.loc.GetString("Conflict_SearchHint"), ref query, 256)) {
            this.presenter.ConflictSearchQuery = query;
        }
        ImGui.Spacing();

        // Already resolved/patched mods grid
        if (ImGui.BeginTable("ResolvedModsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY)) {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColModName"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColState"), ImGuiTableColumnFlags.WidthFixed, 150f);
            ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColAction"), ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableHeadersRow();

            foreach (var kvp in resolvedMods) {
                string modId = kvp.Key;
                var entry = kvp.Value;

                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                if (ImGui.Selectable($"{entry.ModName}##{modId}", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap)) {
                    this.presenter.OpenConflictResolution(modId);
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
    }
}