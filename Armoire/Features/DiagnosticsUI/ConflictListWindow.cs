namespace Armoire.Features.DiagnosticsUI;

using Armoire.Features.ConflictEngine.Models;
using Dalamud.Bindings.ImGui;
using System.Collections.Generic;
using System.Numerics;

public class ConflictListWindow {
    public bool IsVisible { get; set; } = false;
    private List<PenumbraMod> currentConflicts = new();

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

        if (ImGui.Begin("Détails des mods écrasés (Victimes)", ref windowOpen)) {
            if (!windowOpen) {
                this.IsVisible = false;
            }

            ImGui.TextWrapped($"Cette liste affiche uniquement les {this.currentConflicts.Count} mods dont les fichiers sont annulés par des mods de priorité supérieure.");
            ImGui.Spacing();

            if (ImGui.BeginTable("ConflictsTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable)) {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn("Mod Écrasé (Victime)", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Emplacements touchés", ImGuiTableColumnFlags.WidthFixed, 150f);
                ImGui.TableSetupColumn("Écrasé par (Gagnants)", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Priorité", ImGuiTableColumnFlags.WidthFixed, 60f);
                ImGui.TableHeadersRow();

                foreach (var mod in this.currentConflicts) {
                    ImGui.TableNextRow();

                    // Column 1: The Loser Mod
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(mod.Name);

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