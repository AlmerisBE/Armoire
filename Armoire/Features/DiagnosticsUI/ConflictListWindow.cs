namespace Armoire.Features.DiagnosticsUI;

using Armoire.Core.Localization;
using Armoire.Features.ConflictEngine.Models;
using Dalamud.Bindings.ImGui;
using System.Collections.Generic;
using System.Numerics;

public class ConflictListWindow {
    private readonly ILocalizationService loc;
    public bool IsVisible { get; set; } = false;
    private List<PenumbraMod> currentConflicts = new();

    public ConflictListWindow(ILocalizationService localizationService) {
        this.loc = localizationService;
    }

    public void Open() {
        this.IsVisible = true;
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

            if (ImGui.BeginTable("ConflictsTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable)) {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColVictim"), ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColSlots"), ImGuiTableColumnFlags.WidthFixed, 150f);
                ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColWinners"), ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(this.loc.GetString("Conflict_ColPriority"), ImGuiTableColumnFlags.WidthFixed, 60f);
                ImGui.TableHeadersRow();

                foreach (var mod in this.currentConflicts) {
                    ImGui.TableNextRow();

                    // Column 1: The Loser Mod
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(mod.Name);

                    // Column 2: Equipment Slots (To be translated later with Lumina)
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