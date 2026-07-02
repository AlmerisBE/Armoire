namespace Armoire.Features.MainApp.UI.Tabs;

using Armoire.Core.Localization;
using Armoire.Features.DiagnosticsUI.Presentation;
using Armoire.Features.MainApp.Presentation;
using Dalamud.Bindings.ImGui;
using System.Numerics;

public class StatsTab {
    private readonly ILocalizationService loc;
    private readonly IMainWindowPresenter presenter;
    private readonly IPenumbraStatusPresenter statusPresenter;

    public StatsTab(ILocalizationService loc, IMainWindowPresenter presenter, IPenumbraStatusPresenter statusPresenter) {
        this.loc = loc;
        this.presenter = presenter;
        this.statusPresenter = statusPresenter;
    }

    public void Draw() {
        ImGui.Spacing();

        // Render the funnel metrics bars
        ImGui.SetWindowFontScale(1.2f);
        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), this.loc.GetString("StatusView_ModDistribution"));
        ImGui.SetWindowFontScale(1.0f);
        ImGui.Spacing();

        ImGui.TextUnformatted(string.Format(this.loc.GetString("StatusView_TotalIpcMods"), this.statusPresenter.TotalIpcMods));

        ImGui.TextUnformatted(string.Format(this.loc.GetString("StatusView_ConfiguredMods"), this.statusPresenter.ConfiguredMods, this.statusPresenter.ConfiguredModsMax));
        ImGui.ProgressBar(this.statusPresenter.GlobalDirectoryPercentage / 100f, new Vector2(-1, 0), string.Format(this.loc.GetString("StatusView_GlobalDirectory"), this.statusPresenter.GlobalDirectoryPercentage));

        ImGui.TextUnformatted(string.Format(this.loc.GetString("StatusView_EnabledMods"), this.statusPresenter.EnabledMods, this.statusPresenter.EnabledModsMax));
        ImGui.ProgressBar(this.statusPresenter.CollectionPercentage / 100f, new Vector2(-1, 0), string.Format(this.loc.GetString("StatusView_Collection"), this.statusPresenter.CollectionPercentage));

        ImGui.TextUnformatted(string.Format(this.loc.GetString("StatusView_ConflictingMods"), this.statusPresenter.ConflictingMods, this.statusPresenter.ConflictingModsMax));
        ImGui.ProgressBar(this.statusPresenter.ActiveModsPercentage / 100f, new Vector2(-1, 0), string.Format(this.loc.GetString("StatusView_ActiveMods"), this.statusPresenter.ActiveModsPercentage));

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        // Lifetime counters
        ImGui.TextColored(new Vector4(0.2f, 1.0f, 0.2f, 1.0f), string.Format(this.loc.GetString("Main_StatsResolved"), this.presenter.ResolvedConflictsCount));

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        // Active collection architecture hierarchy tree list
        ImGui.SetWindowFontScale(1.2f);
        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), this.loc.GetString("StatusView_Hierarchy"));
        ImGui.SetWindowFontScale(1.0f);
        if (this.statusPresenter.ActiveCollections.Count == 0) {
            ImGui.TextDisabled(this.loc.GetString("StatusView_NoActiveCollection"));
        } else {
            foreach (var node in this.statusPresenter.ActiveCollections) {
                string prefix = node.IsInherited
                    ? this.loc.GetString("StatusView_InheritedPrefix")
                    : this.loc.GetString("StatusView_ActivePrefix");
                ImGui.TextUnformatted($"{prefix}{node.Name}");
            }
        }
    }
}