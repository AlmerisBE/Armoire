namespace Armoire.Features.MainApp.UI.Tabs;

using Armoire.Core.Localization;
using Armoire.Features.DiagnosticsUI.Presentation;
using Armoire.Features.MainApp.Presentation;
using Armoire.Features.Outfits.Presentation;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using System.Linq;
using System.Numerics;

public class StatsTab {
    private readonly ILocalizationService loc;
    private readonly IMainWindowPresenter presenter;
    private readonly IPenumbraStatusPresenter statusPresenter;
    private readonly IOutfitsPresenter outfitsPresenter;

    public StatsTab(ILocalizationService loc, IMainWindowPresenter presenter, IPenumbraStatusPresenter statusPresenter, IOutfitsPresenter outfitsPresenter) {
        this.loc = loc;
        this.presenter = presenter;
        this.statusPresenter = statusPresenter;
        this.outfitsPresenter = outfitsPresenter;
    }

    public void Draw() {
        ImGui.Spacing();

        // --- 1. DISTRIBUTION DES MODS ---
        ImGui.SetWindowFontScale(1.2f);
        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), this.loc.GetString("StatusView_ModDistribution"));
        ImGui.SetWindowFontScale(1.0f);
        ImGui.Spacing();

        ImGui.TextUnformatted(string.Format(this.loc.GetString("StatusView_TotalIpcMods"), this.statusPresenter.TotalIpcMods));

        ImGui.TextUnformatted(string.Format(this.loc.GetString("StatusView_ConfiguredMods"), this.statusPresenter.ConfiguredMods, this.statusPresenter.ConfiguredModsMax));
        ImGui.ProgressBar(this.statusPresenter.GlobalDirectoryPercentage / 100f, new Vector2(-1, 0), string.Format(this.loc.GetString("StatusView_GlobalDirectory"), this.statusPresenter.GlobalDirectoryPercentage.ToString("0.00")));

        ImGui.TextUnformatted(string.Format(this.loc.GetString("StatusView_EnabledMods"), this.statusPresenter.EnabledMods, this.statusPresenter.EnabledModsMax));
        ImGui.ProgressBar(this.statusPresenter.CollectionPercentage / 100f, new Vector2(-1, 0), string.Format(this.loc.GetString("StatusView_Collection"), this.statusPresenter.CollectionPercentage.ToString("0.00")));

        ImGui.TextUnformatted(string.Format(this.loc.GetString("StatusView_ConflictingMods"), this.statusPresenter.ConflictingMods, this.statusPresenter.ConflictingModsMax));
        ImGui.ProgressBar(this.statusPresenter.ActiveModsPercentage / 100f, new Vector2(-1, 0), string.Format(this.loc.GetString("StatusView_ActiveMods"), this.statusPresenter.ActiveModsPercentage.ToString("0.00")));

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        // --- 2. STATISTIQUES DES GARDE-ROBES (NOUVEAU) ---
        ImGui.SetWindowFontScale(1.2f);
        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), this.loc.GetString("Stats_OutfitsTitle"));
        ImGui.SetWindowFontScale(1.0f);
        ImGui.Spacing();

        int totalOutfits = this.outfitsPresenter.Outfits.Count;
        int readyOutfits = this.outfitsPresenter.Outfits.Count(o => this.outfitsPresenter.CheckOutfitReadiness(o).IsReady);

        ImGui.TextUnformatted(string.Format(this.loc.GetString("Stats_TotalOutfits"), totalOutfits));

        if (totalOutfits > 0) {
            float readyPct = (float)readyOutfits / totalOutfits;
            ImGui.ProgressBar(readyPct, new Vector2(-1, 0), string.Format(this.loc.GetString("Stats_ReadyOutfits"), readyOutfits, totalOutfits, readyPct * 100f));
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        // --- 3. COMPTEUR À VIE ---
        ImGui.TextColored(new Vector4(0.2f, 1.0f, 0.2f, 1.0f), string.Format(this.loc.GetString("Main_StatsResolved"), this.presenter.ResolvedConflictsCount));

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        // --- 4. HIÉRARCHIE DE LA COLLECTION ---
        ImGui.SetWindowFontScale(1.2f);
        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), this.loc.GetString("StatusView_Hierarchy"));
        ImGui.SetWindowFontScale(1.0f);
        ImGui.Spacing();

        if (this.statusPresenter.ActiveCollections.Count == 0) {
            ImGui.TextDisabled(this.loc.GetString("StatusView_NoActiveCollection"));
        } else {
            foreach (var node in this.statusPresenter.ActiveCollections) {
                FontAwesomeIcon icon = node.IsInherited ? FontAwesomeIcon.AngleRight : FontAwesomeIcon.CaretRight;

                ImGui.PushFont(Dalamud.Interface.UiBuilder.IconFont);
                ImGui.TextUnformatted(icon.ToIconString());
                ImGui.PopFont();

                ImGui.SameLine();
                ImGui.TextUnformatted(node.Name);
            }
        }
    }
}