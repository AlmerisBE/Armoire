namespace Armoire.Features.DiagnosticsUI;

using Armoire.Core.Localization;
using Armoire.Features.ConflictEngine.Models;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class ConflictListWindow {
    private readonly ILocalizationService loc;
    public bool IsVisible { get; set; } = false;

    private List<PenumbraMod> currentConflicts = new();
    private string searchQuery = string.Empty;
    private readonly Armoire.Features.ModDetails.UI.ModDetailsWindow modDetailsWindow;

    // Mets à jour le constructeur
    public ConflictListWindow(ILocalizationService localizationService, Armoire.Features.ModDetails.UI.ModDetailsWindow detailsWindow) {
        this.loc = localizationService;
        this.modDetailsWindow = detailsWindow;
    }

    public void Open() {
        this.IsVisible = true;
        this.searchQuery = string.Empty;
    }

    public void Draw(List<PenumbraMod> liveConflicts) {
        if (!this.IsVisible) {
            return;
        }

        this.currentConflicts = liveConflicts ?? new List<PenumbraMod>();

        bool windowOpen = this.IsVisible;
        ImGui.SetNextWindowSize(new Vector2(900, 500), ImGuiCond.FirstUseEver);

        if (ImGui.Begin(this.loc.GetString("Conflict_WindowTitle"), ref windowOpen)) {
            if (!windowOpen) {
                this.IsVisible = false;
            }

            ImGui.TextWrapped(string.Format(this.loc.GetString("Conflict_Description"), this.currentConflicts.Count));
            ImGui.Spacing();

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("##SearchConflict", this.loc.GetString("Conflict_SearchHint"), ref this.searchQuery, 256);
            ImGui.Spacing();

            var filteredConflicts = string.IsNullOrWhiteSpace(this.searchQuery)
                ? this.currentConflicts
                : this.currentConflicts.Where(m =>
                    m.Name.Contains(this.searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    m.OverwrittenBy.Any(winner => winner.Contains(this.searchQuery, StringComparison.OrdinalIgnoreCase))
                  ).ToList();

            if (ImGui.BeginTable("ConflictsTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable)) {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColVictim"), ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColSlots"), ImGuiTableColumnFlags.WidthFixed, 150f);
                ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColWinners"), ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColPriority"), ImGuiTableColumnFlags.WidthFixed, 60f);
                ImGui.TableHeadersRow();

                foreach (var mod in filteredConflicts) {
                    ImGui.TableNextRow();

                    // Column 1: The Loser Mod
                    ImGui.TableNextColumn();
                    if (ImGui.Selectable($"{mod.Name}##{mod.Id}", false, ImGuiSelectableFlags.SpanAllColumns)) {
                        this.modDetailsWindow.Open(mod.Id);
                    }

                    // Column 2: Equipment Slots
                    ImGui.TableNextColumn();
                    string slots = string.Join(", ", mod.ConflictingSlots);
                    ImGui.TextWrapped(slots);

                    // Column 3: The Winner Mods
                    ImGui.TableNextColumn();
                    string overwrittenBy = string.Join(", ", mod.OverwrittenBy);
                    ImGui.TextColored(new Vector4(1.0f, 0.4f, 0.0f, 1.0f), overwrittenBy);

                    // Column 4: Priority of the Loser
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(mod.Priority.ToString());
                }
                ImGui.EndTable();
            }
        }
        ImGui.End();
    }
}