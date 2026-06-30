namespace Armoire.Features.DiagnosticsUI;

using Armoire.Core.Localization;
using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.ModSwapper;
using Armoire.Features.PenumbraIpc;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

public class ConflictListWindow {
    private readonly ILocalizationService loc;
    public bool IsVisible { get; set; } = false;

    private List<PenumbraMod> currentConflicts = new();
    private string searchQuery = string.Empty;
    private readonly Armoire.Features.ModDetails.UI.ModDetailsWindow modDetailsWindow;

    private readonly ArmoireConfiguration config;
    private readonly IModSwapperService swapperService;
    private readonly IPenumbraClient penumbraClient;

    public ConflictListWindow(
        ILocalizationService localizationService,
        Armoire.Features.ModDetails.UI.ModDetailsWindow detailsWindow,
        ArmoireConfiguration config,
        IModSwapperService swapperService,
        IPenumbraClient penumbraClient) {
        this.loc = localizationService;
        this.modDetailsWindow = detailsWindow;
        this.config = config;
        this.swapperService = swapperService;
        this.penumbraClient = penumbraClient;
    }

    public void Open() {
        this.IsVisible = true;
        this.searchQuery = string.Empty;
    }

    private bool IsModActivelyPatched(string modId) {
        string rootDir = this.penumbraClient.GetModDirectory();
        if (string.IsNullOrEmpty(rootDir)) {
            return false;
        }

        string fullModPath = Path.Combine(rootDir, modId);
        if (!Directory.Exists(fullModPath)) {
            return false;
        }

        var backupFiles = Directory.GetFiles(fullModPath, "*.armoire_bak", SearchOption.TopDirectoryOnly);
        return backupFiles.Length > 0;
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

            // Champ de recherche unique et global
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("##SearchConflict", this.loc.GetString("Conflict_SearchHint"), ref this.searchQuery, 256);
            ImGui.Spacing();

            // --- SECTION 1: MODS MODIFIÉS PAR ARMOIRE (FILTRÉE) ---
            DrawModifiedModsSection();

            // --- SECTION 2: CONFLITS VANILLA (FILTRÉE) ---
            DrawConflictsSection();
        }
        ImGui.End();
    }

    private void DrawModifiedModsSection() {
        if (this.config.ModifiedMods.Count == 0) {
            return;
        }

        // APPLICATION DU FILTRE SUR LA LISTE DU HAUT
        var filteredModifiedMods = this.config.ModifiedMods
            .Where(kvp => string.IsNullOrWhiteSpace(this.searchQuery) ||
                          kvp.Value.ModName.Contains(this.searchQuery, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Si aucun mod modifié ne correspond à la recherche, on masque proprement la section
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
                    this.modDetailsWindow.Open(modId);
                }
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip(string.Format(this.loc.GetString("Conflict_TooltipSwaps"), modId, entry.Swaps.Count));
                }

                ImGui.TableNextColumn();
                bool isPatched = IsModActivelyPatched(modId);

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
                        this.swapperService.ResetMod(modId);
                    }
                } else {
                    if (ImGui.Button($"{this.loc.GetString("Conflict_BtnRestore")}##{modId}")) {
                        foreach (var swapInfo in entry.Swaps) {
                            this.swapperService.PerformSwap(modId, swapInfo.Key, swapInfo.Value);
                        }
                    }
                }
            }
            ImGui.EndTable();
        }
        ImGui.Spacing();
        ImGui.Spacing();
    }

    private void DrawConflictsSection() {
        ImGui.TextColored(new Vector4(1.0f, 0.2f, 0.2f, 1.0f), this.loc.GetString("Conflict_Title_List"));
        ImGui.Separator();

        var filteredConflicts = this.currentConflicts
            .Where(m => !this.config.ModifiedMods.ContainsKey(m.Id))
            .Where(m => string.IsNullOrWhiteSpace(this.searchQuery) ||
                        m.Name.Contains(this.searchQuery, StringComparison.OrdinalIgnoreCase) ||
                        m.OverwrittenBy.Any(winner => winner.Contains(this.searchQuery, StringComparison.OrdinalIgnoreCase)))
            .ToList();

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
}