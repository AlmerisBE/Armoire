namespace Armoire.Features.MainApp.UI.Tabs;

using Armoire.Core.Localization;
using Armoire.Features.Outfits.Presentation;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components; // <-- NEW: Required for IconButtonWithText
using System.Linq;
using System.Numerics;

public class OutfitsTab {
    private readonly ILocalizationService loc;
    private readonly IOutfitsPresenter presenter;
    private readonly IOutfitDetailsPresenter detailsPresenter;
    private readonly IOutfitImportPresenter importPresenter;
    private string newOutfitName = string.Empty;

    public OutfitsTab(ILocalizationService loc, IOutfitsPresenter presenter, IOutfitDetailsPresenter detailsPresenter, IOutfitImportPresenter importPresenter) {
        this.loc = loc;
        this.presenter = presenter;
        this.detailsPresenter = detailsPresenter;
        this.importPresenter = importPresenter;
    }

    public void Draw() {
        ImGui.Spacing();

        // --- SECTION 1: CREATION ---
        ImGui.SetWindowFontScale(1.2f);
        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), this.loc.GetString("Outfits_CreateTitle"));
        ImGui.SetWindowFontScale(1.0f);

        // Reduced width to let the UI breathe
        ImGui.SetNextItemWidth(250f);
        ImGui.InputTextWithHint("##NewOutfitName", this.loc.GetString("Outfits_NameHint"), ref this.newOutfitName, 64);
        ImGui.SameLine();

        // Add a "Camera" icon to the capture button using Dalamud's native component
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Camera, this.loc.GetString("Outfits_CaptureBtn"))) {
            if (!string.IsNullOrWhiteSpace(this.newOutfitName)) {
                this.presenter.CreateOutfit(this.newOutfitName);
                this.newOutfitName = string.Empty;
            }
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        // --- SECTION 2: LIST AND IMPORT ---
        ImGui.SetWindowFontScale(1.2f);
        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), this.loc.GetString("Outfits_ListTitle"));
        ImGui.SetWindowFontScale(1.0f);

        // The Import button is now right-aligned on the same line as the list title
        string importText = this.loc.GetString("Import_WindowTitle");
        // Approximate width: Icon size (~24px) + spacing + text size + padding
        float importBtnWidth = 24f + ImGui.GetStyle().ItemSpacing.X + ImGui.CalcTextSize(importText).X + (ImGui.GetStyle().FramePadding.X * 2);

        float alignX = ImGui.GetWindowContentRegionMax().X - importBtnWidth;
        if (alignX > ImGui.GetCursorPosX()) {
            ImGui.SameLine(alignX);
        } else {
            // Fallback if the window is exceptionally narrow
            ImGui.SameLine();
        }

        // Draw the Import button using Dalamud's native component
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.FileImport, importText)) {
            this.importPresenter.Open();
        }

        ImGui.Spacing();

        // --- OUTFITS TABLE ---
        if (this.presenter.Outfits.Count == 0) {
            ImGui.TextDisabled(this.loc.GetString("Outfits_EmptyList"));
            return;
        }

        if (ImGui.BeginTable("OutfitsLibraryTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, 250))) {
            try {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn(this.loc.GetString("Outfits_ColName"), ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(this.loc.GetString("Outfits_ColMods"), ImGuiTableColumnFlags.WidthFixed, 160f);
                ImGui.TableSetupColumn(this.loc.GetString("Outfits_ColActions"), ImGuiTableColumnFlags.WidthFixed, 220f);
                ImGui.TableHeadersRow();

                foreach (var outfit in this.presenter.Outfits.ToList()) {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    if (ImGui.Selectable($"{outfit.Name}##select_{outfit.Id}", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap)) {
                        this.detailsPresenter.Open(outfit);
                    }
                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip(this.loc.GetString("Outfits_TooltipInspect"));
                    }

                    // Column 2: Readiness Status
                    ImGui.TableNextColumn();
                    var readiness = this.presenter.CheckOutfitReadiness(outfit);
                    int activeReqsCount = outfit.RequiredMods?.Count(r => !r.IsIgnored) ?? 0;

                    if (activeReqsCount == 0) {
                        ImGui.TextDisabled(this.loc.GetString("Outfits_StatusNoMods"));
                    } else if (readiness.IsReady) {
                        DrawIconText(FontAwesomeIcon.CheckCircle, new Vector4(0.2f, 1.0f, 0.2f, 1.0f), string.Format(this.loc.GetString("Outfits_StatusReady"), activeReqsCount));
                    } else {
                        DrawIconText(FontAwesomeIcon.ExclamationTriangle, new Vector4(1.0f, 0.6f, 0.0f, 1.0f), string.Format(this.loc.GetString("Outfits_StatusMissing"), readiness.MissingModNames.Count));
                        if (ImGui.IsItemHovered()) {
                            ImGui.SetTooltip(this.loc.GetString("Outfits_TooltipMissing") + string.Join("\n", readiness.MissingModNames.Select(m => $"- {m}")));
                        }
                    }

                    // Column 3: Actions
                    ImGui.TableNextColumn();

                    // Dynamic exact right alignment for the action buttons
                    float b1 = ImGui.CalcTextSize(this.loc.GetString("Outfits_BtnActivate")).X + ImGui.GetStyle().FramePadding.X * 2;
                    float b2 = ImGui.CalcTextSize(this.loc.GetString("Outfits_BtnShare")).X + ImGui.GetStyle().FramePadding.X * 2;
                    float b3 = ImGui.CalcTextSize(this.loc.GetString("Outfits_BtnDelete")).X + ImGui.GetStyle().FramePadding.X * 2;
                    float spacing = ImGui.GetStyle().ItemSpacing.X;
                    float totalW = b1 + b2 + b3 + (spacing * 2);

                    float availX = ImGui.GetContentRegionAvail().X;
                    if (availX > totalW) {
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + availX - totalW);
                    }

                    if (ImGui.Button($"{this.loc.GetString("Outfits_BtnActivate")}##{outfit.Id}")) {
                        this.presenter.ActivateOutfit(outfit);
                    }
                    ImGui.SameLine();

                    if (ImGui.Button($"{this.loc.GetString("Outfits_BtnShare")}##{outfit.Id}")) {
                        this.presenter.ExportToClipboard(outfit);
                    }
                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip(this.loc.GetString("Outfits_TooltipShare"));
                    }
                    ImGui.SameLine();

                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1.0f, 0.3f, 0.3f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.1f, 0.1f, 1.0f));
                    if (ImGui.Button($"{this.loc.GetString("Outfits_BtnDelete")}##{outfit.Id}")) {
                        this.presenter.DeleteOutfit(outfit.Id);
                    }
                    ImGui.PopStyleColor(3);
                }
            } finally {
                ImGui.EndTable();
            }
        }
    }

    private void DrawIconText(FontAwesomeIcon icon, Vector4 color, string text) {
        ImGui.PushFont(Dalamud.Interface.UiBuilder.IconFont);
        ImGui.TextColored(color, icon.ToIconString());
        ImGui.PopFont();
        ImGui.SameLine();
        ImGui.TextColored(color, text);
    }
}