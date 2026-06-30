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
            // Reduced to 3 columns: Victim, Winners, and Action Button
            if (ImGui.BeginTable("ConflictsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable)) {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColVictim"), ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColWinners"), ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColAction"), ImGuiTableColumnFlags.WidthFixed, 150f);
                ImGui.TableHeadersRow();

                foreach (var mod in this.presenter.CurrentConflicts) {
                    ImGui.TableNextRow();

                    // Column 1: Victim Mod Name
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(mod.Name);

                    // Column 2: Overwritten By (Winners)
                    ImGui.TableNextColumn();
                    ImGui.TextColored(new Vector4(1.0f, 0.4f, 0.0f, 1.0f), string.Join(", ", mod.OverwrittenBy));

                    // Column 3: Resolution Action Button
                    ImGui.TableNextColumn();
                    if (ImGui.Button($"{this.loc.GetString("Main_HomeResolveBtn")}##{mod.Id}")) {
                        // Triggers the opening of the ModDetailsWindow via the presenter
                        this.presenter.OpenConflictResolution(mod.Id);
                    }
                }
                ImGui.EndTable();
            }
        } else {
            ImGui.TextColored(new Vector4(0.2f, 1.0f, 0.2f, 1.0f), "Tous vos mods actifs fonctionnent parfaitement ! Aucun conflit détecté.");
        }
    }
}