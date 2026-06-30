namespace Armoire.Features.MainApp.UI.Tabs;

using Armoire.Core.Localization;
using Armoire.Features.MainApp.Presentation;
using Dalamud.Bindings.ImGui;
using System.Numerics;

public class HomeTab {
    private readonly ILocalizationService loc;
    private readonly IMainWindowPresenter presenter;

    public HomeTab(ILocalizationService loc, IMainWindowPresenter presenter) {
        this.loc = loc;
        this.presenter = presenter;
    }

    public void Draw() {
        ImGui.Spacing();

        // Render the collection summary sentence
        ImGui.TextWrapped(string.Format(this.loc.GetString("Main_HomeSummary"),
            this.presenter.TotalInstalledMods,
            this.presenter.ConfiguredMods,
            this.presenter.ActiveMods,
            this.presenter.MainCollectionName));

        ImGui.Spacing();

        if (this.presenter.IgnoredConflictsCount > 0) {
            ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.0f, 1.0f), string.Format(this.loc.GetString("Main_HomeConflicts"), this.presenter.IgnoredConflictsCount));
            ImGui.Spacing();

            // Conflict filtering query input
            string query = this.presenter.ConflictSearchQuery;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.InputTextWithHint("##SearchConflict", this.loc.GetString("Conflict_SearchHint"), ref query, 256)) {
                this.presenter.ConflictSearchQuery = query;
            }
            ImGui.Spacing();

            // Active remaining conflicts list
            if (ImGui.BeginTable("ConflictsTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable)) {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColVictim"), ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColSlots"), ImGuiTableColumnFlags.WidthFixed, 150f);
                ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColWinners"), ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColPriority"), ImGuiTableColumnFlags.WidthFixed, 60f);
                ImGui.TableHeadersRow();

                foreach (var mod in this.presenter.CurrentConflicts) {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    if (ImGui.Selectable($"{mod.Name}##{mod.Id}", false, ImGuiSelectableFlags.SpanAllColumns)) {
                        this.presenter.OpenConflictResolution(mod.Id);
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
        } else {
            ImGui.TextColored(new Vector4(0.2f, 1.0f, 0.2f, 1.0f), "Tous vos mods actifs fonctionnent parfaitement ! Aucun conflit détecté.");
        }
    }
}