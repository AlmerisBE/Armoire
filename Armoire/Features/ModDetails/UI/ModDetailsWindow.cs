namespace Armoire.Features.ModDetails.UI;

using Armoire.Core.Localization;
using Armoire.Features.ConflictEngine;
using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.ModDetails;
using Armoire.Features.ModDetails.Models;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using System;
using System.Linq;
using System.Numerics;

public class ModDetailsWindow : IDisposable {
    private readonly IModDetailsResolver resolver;
    private readonly IPenumbraSyncManager syncManager;
    private readonly ILocalizationService loc;
    private readonly ITextureProvider textureProvider;

    public bool IsVisible { get; set; } = false;
    private string currentModId = string.Empty;
    private DetailedModState? currentState = null;

    public ModDetailsWindow(
        IModDetailsResolver resolver,
        IPenumbraSyncManager syncManager,
        ILocalizationService loc,
        ITextureProvider textureProvider
    ) {
        this.resolver = resolver;
        this.syncManager = syncManager;
        this.loc = loc;
        this.textureProvider = textureProvider;

        this.syncManager.OnStatusUpdated += RefreshData;
    }

    public void Open(string modId) {
        this.currentModId = modId;
        this.syncManager.ForceRefresh();
        this.IsVisible = true;
    }

    private void RefreshData(PenumbraStatusResult fullStatus) {
        if (!this.IsVisible || string.IsNullOrEmpty(this.currentModId)) {
            return;
        }
    }

    public void Draw(EffectiveCollectionState? globalState) {
        if (!this.IsVisible) {
            return;
        }

        if (!string.IsNullOrEmpty(this.currentModId) && globalState != null) {
            this.currentState = this.resolver.ResolveModDetails(this.currentModId, globalState);
        }

        bool windowOpen = this.IsVisible;
        ImGui.SetNextWindowSize(new Vector2(900, 650), ImGuiCond.FirstUseEver);

        string modNameStr = this.currentState?.ModName ?? this.loc.GetString("ModDetails_Loading");
        string windowTitle = string.Format(this.loc.GetString("ModDetails_WindowTitle"), modNameStr) + "###ModDetails";

        if (ImGui.Begin(windowTitle, ref windowOpen)) {
            if (!windowOpen) {
                this.IsVisible = false;
            }

            if (this.currentState == null) {
                ImGui.Text(this.loc.GetString("ModDetails_Unavailable"));
                ImGui.End();
                return;
            }

            string stateStr = this.currentState.IsEnabled ? this.loc.GetString("ModDetails_StateEnabled") : this.loc.GetString("ModDetails_StateDisabled");
            ImGui.TextWrapped(string.Format(this.loc.GetString("ModDetails_Header"), this.currentState.Priority, stateStr));
            ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

            if (ImGui.BeginTabBar("ModDetailsTabs")) {

                if (ImGui.BeginTabItem("Résumé (Fiche d'équipement)")) {
                    DrawCharacterSheetView();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Fichiers (Vue Avancée)")) {
                    DrawAdvancedFilesView();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }
        ImGui.End();
    }

    private void DrawCharacterSheetView() {
        ImGui.Spacing();

        if (ImGui.BeginTable("CharacterSheetTable", 2, ImGuiTableFlags.None)) {
            ImGui.TableSetupColumn("LeftEquipment", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("RightEquipment", ImGuiTableColumnFlags.WidthStretch);

            // Row 1: Main Weapon | Off-Hand
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); DrawSpecificSlot("wpn", this.loc.GetString("Slot_MainHand"));
            ImGui.TableNextColumn(); DrawSpecificSlot("sub", this.loc.GetString("Slot_OffHand"));

            // Row 2: Head | Earrings
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); DrawSpecificSlot("met", this.loc.GetString("Slot_Head"));
            ImGui.TableNextColumn(); DrawSpecificSlot("ear", this.loc.GetString("Slot_Earrings"));

            // Row 3: Body | Necklace
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); DrawSpecificSlot("top", this.loc.GetString("Slot_Body"));
            ImGui.TableNextColumn(); DrawSpecificSlot("nek", this.loc.GetString("Slot_Necklace"));

            // Row 4: Hands | Bracelets
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); DrawSpecificSlot("glv", this.loc.GetString("Slot_Hands"));
            ImGui.TableNextColumn(); DrawSpecificSlot("wrs", this.loc.GetString("Slot_Bracelets"));

            // Row 5: Legs | Ring (Right)
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); DrawSpecificSlot("dwn", this.loc.GetString("Slot_Legs"));
            ImGui.TableNextColumn(); DrawSpecificSlot("rir", this.loc.GetString("Slot_RingRight"));

            // Row 6: Feet | Ring (Left)
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); DrawSpecificSlot("sho", this.loc.GetString("Slot_Feet"));
            ImGui.TableNextColumn(); DrawSpecificSlot("ril", this.loc.GetString("Slot_RingLeft"));

            ImGui.EndTable();
        }

        // Customization and System files
        ImGui.Spacing();
        DrawSpecificSlot("custom", "Customisation");
        DrawSpecificSlot("unknown", "Fichiers Système");

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        if (ImGui.Button("Réinitialiser le mod (Paramètres par défaut)")) {
            // Future action
        }
    }

    private void DrawSpecificSlot(string slotKey, string slotDisplayName) {
        var itemsInSlot = this.currentState!.ReplacedSlots
            .Where(s => s.SlotCategory == slotKey)
            .GroupBy(s => new {
                s.IconId,
                BaseName = s.LocalizedItemName.Split('[')[0].Trim()
            })
            .Select(g => new {
                IconId = g.Key.IconId,
                BaseName = g.Key.BaseName,
                IsConflicting = g.Any(x => x.IsConflicting),
                OverwrittenByMods = g.SelectMany(x => x.OverwrittenByMods).Distinct().ToList()
            })
            .ToList();

        // CAS 1 : Aucun fichier du mod ne modifie cet emplacement
        if (!itemsInSlot.Any()) {
            // On ne dessine pas les lignes vides pour les catégories spéciales (custom, unknown)
            if (slotKey == "custom" || slotKey == "unknown") {
                return;
            }

            ImGui.PushID($"empty_{slotKey}");

            // Dessin d'un faux "Slot" vide façon FFXIV (Carré semi-transparent avec bordure)
            Vector2 p = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddRectFilled(p, p + new Vector2(40, 40), ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 0.5f)), 4f);
            ImGui.GetWindowDrawList().AddRect(p, p + new Vector2(40, 40), ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.3f, 0.3f, 1f)), 4f);
            ImGui.Dummy(new Vector2(40, 40));

            ImGui.SameLine();
            Vector2 cursorPos = ImGui.GetCursorPos();

            ImGui.SetCursorPos(new Vector2(cursorPos.X, cursorPos.Y + 4));
            ImGui.TextDisabled(slotDisplayName);

            ImGui.SetCursorPos(new Vector2(cursorPos.X, cursorPos.Y + 20));
            ImGui.TextDisabled($"({this.loc.GetString("Slot_Unmodified")})");

            ImGui.PopID();
            ImGui.Spacing();
            return;
        }

        // CAS 2 : Le mod modifie cet emplacement (On dessine les objets)
        foreach (var item in itemsInSlot) {
            ImGui.PushID(item.BaseName + item.IconId);

            if (item.IconId > 0) {
                var iconWrap = this.textureProvider.GetFromGameIcon(new GameIconLookup(item.IconId)).GetWrapOrDefault();
                if (iconWrap != null) {
                    ImGui.Image(iconWrap.Handle, new Vector2(40, 40));
                } else {
                    ImGui.Dummy(new Vector2(40, 40));
                }
            } else {
                ImGui.Dummy(new Vector2(40, 40));
            }

            ImGui.SameLine();
            Vector2 cursorPos = ImGui.GetCursorPos();

            if (item.IsConflicting) {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.4f, 0.4f, 1));
            }

            if (ImGui.Selectable($"##select_{item.BaseName}", false, ImGuiSelectableFlags.None, new Vector2(0, 40))) {
                // Action future : Ouvrir le module de Swap
            }
            if (item.IsConflicting) {
                ImGui.PopStyleColor();
            }

            ImGui.SetCursorPos(new Vector2(cursorPos.X, cursorPos.Y + 4));
            ImGui.TextUnformatted(item.BaseName);

            if (item.IsConflicting) {
                ImGui.SetCursorPos(new Vector2(cursorPos.X, cursorPos.Y + 20));
                ImGui.TextDisabled($"(En conflit avec : {string.Join(", ", item.OverwrittenByMods)})");
            } else {
                ImGui.SetCursorPos(new Vector2(cursorPos.X, cursorPos.Y + 20));
                ImGui.TextColored(new Vector4(0.4f, 1, 0.4f, 1), "(Actif et appliqué)");
            }

            ImGui.PopID();
            ImGui.Spacing();
        }
    }

    private void DrawAdvancedFilesView() {
        ImGui.Spacing();
        if (ImGui.BeginTable("ModDetailsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable)) {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn(this.loc.GetString("ModDetails_ColItem"), ImGuiTableColumnFlags.WidthFixed, 200f);
            ImGui.TableSetupColumn(this.loc.GetString("ModDetails_ColStatus"), ImGuiTableColumnFlags.WidthFixed, 150f);
            ImGui.TableSetupColumn(this.loc.GetString("ModDetails_ColWinner"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (var slot in this.currentState!.ReplacedSlots) {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                string displayTitle = slot.AffectedPaths.Count > 1
                    ? $"{slot.LocalizedItemName} (x{slot.AffectedPaths.Count})"
                    : slot.LocalizedItemName;
                ImGui.TextUnformatted(displayTitle);

                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip(string.Join("\n", slot.AffectedPaths));
                }

                ImGui.TableNextColumn();
                if (slot.IsConflicting) {
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), this.loc.GetString("ModDetails_StatusConflict"));
                } else {
                    ImGui.TextColored(new Vector4(0, 1, 0, 1), this.loc.GetString("ModDetails_StatusActive"));
                }

                ImGui.TableNextColumn();
                if (slot.IsConflicting) {
                    ImGui.TextWrapped(string.Join(", ", slot.OverwrittenByMods));
                } else {
                    ImGui.TextDisabled("-");
                }
            }
            ImGui.EndTable();
        }
    }

    public void Dispose() {
        this.syncManager.OnStatusUpdated -= RefreshData;
    }
}