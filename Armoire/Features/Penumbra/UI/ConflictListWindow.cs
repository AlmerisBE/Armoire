namespace Armoire.Features.Penumbra.UI;

using Armoire.Features.Penumbra.Core.Domain;
using Dalamud.Bindings.ImGui;
using System.Collections.Generic;
using System.Numerics;

public class ConflictListWindow {
    public bool IsVisible { get; set; } = false;
    private List<PenumbraMod> currentConflicts = new();

    // Injects the latest conflict list when opening
    public void Open(List<PenumbraMod> conflicts) {
        this.currentConflicts = conflicts ?? new List<PenumbraMod>();
        this.IsVisible = true;
    }

    public void Draw() {
        if (!this.IsVisible) {
            return;
        }

        bool windowOpen = this.IsVisible;
        ImGui.SetNextWindowSize(new Vector2(650, 450), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Détails des mods en conflit", ref windowOpen)) {
            if (!windowOpen) {
                this.IsVisible = false;
            }

            ImGui.TextWrapped($"Cette liste contient les {this.currentConflicts.Count} mods dont au moins un fichier est écrasé, ou qui écrase un autre mod prioritaire.");
            ImGui.Spacing();

            // Render a high-performance ImGui table with headers and borders
            if (ImGui.BeginTable("ConflictsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable)) {

                // Keep headers visible while scrolling
                ImGui.TableSetupScrollFreeze(0, 1);

                ImGui.TableSetupColumn("Nom du Mod", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Priorité", ImGuiTableColumnFlags.WidthFixed, 60f);
                ImGui.TableSetupColumn("Collection Source", ImGuiTableColumnFlags.WidthFixed, 150f);
                ImGui.TableHeadersRow();

                foreach (var mod in this.currentConflicts) {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(mod.Name);

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(mod.Priority.ToString());

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(mod.SourceCollectionName);
                }
                ImGui.EndTable();
            }
        }
        ImGui.End();
    }
}