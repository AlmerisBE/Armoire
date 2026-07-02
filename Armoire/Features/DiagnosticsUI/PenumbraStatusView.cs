namespace Armoire.Features.DiagnosticsUI;

using Armoire.Core.Localization;
using Armoire.Core.UI;
using Armoire.Features.DiagnosticsUI.Presentation;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using System.Numerics;

public class PenumbraStatusView : IUiComponent {
    private readonly ILocalizationService loc;
    private readonly IPenumbraStatusPresenter presenter;

    public PenumbraStatusView(ILocalizationService loc, IPenumbraStatusPresenter presenter) {
        this.loc = loc;
        this.presenter = presenter;
    }

    public void Draw() {
        // Integration Status & Character
        string statusLoc = this.presenter.IntegrationStatus == "Active"
            ? this.loc.GetString("StatusView_Active")
            : this.loc.GetString("StatusView_Inactive");

        ImGui.TextUnformatted(string.Format(this.loc.GetString("StatusView_IntegrationStatus"), statusLoc));

        string charLoc = string.IsNullOrEmpty(this.presenter.ConnectedCharacter) || this.presenter.ConnectedCharacter == "None"
            ? this.loc.GetString("StatusView_None")
            : this.presenter.ConnectedCharacter;

        ImGui.TextUnformatted(string.Format(this.loc.GetString("StatusView_ConnectedCharacter"), charLoc));

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        if (this.presenter.IsBgScanInProgress) {
            ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.0f, 1.0f), this.loc.GetString("StatusView_BgScanInProgress"));
            ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        }

        // Mod Distribution Section
        ImGui.SetWindowFontScale(1.2f);
        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), this.loc.GetString("StatusView_ModDistribution"));
        ImGui.SetWindowFontScale(1.0f);
        ImGui.Spacing();

        ImGui.TextUnformatted(string.Format(this.loc.GetString("StatusView_TotalIpcMods"), this.presenter.TotalIpcMods));

        ImGui.TextUnformatted(string.Format(this.loc.GetString("StatusView_ConfiguredMods"), this.presenter.ConfiguredMods, this.presenter.ConfiguredModsMax));
        ImGui.ProgressBar(this.presenter.GlobalDirectoryPercentage / 100f, new Vector2(-1, 0), string.Format(this.loc.GetString("StatusView_GlobalDirectory"), this.presenter.GlobalDirectoryPercentage.ToString("0.00")));

        ImGui.TextUnformatted(string.Format(this.loc.GetString("StatusView_EnabledMods"), this.presenter.EnabledMods, this.presenter.EnabledModsMax));
        ImGui.ProgressBar(this.presenter.CollectionPercentage / 100f, new Vector2(-1, 0), string.Format(this.loc.GetString("StatusView_Collection"), this.presenter.CollectionPercentage.ToString("0.00")));

        ImGui.TextUnformatted(string.Format(this.loc.GetString("StatusView_ConflictingMods"), this.presenter.ConflictingMods, this.presenter.ConflictingModsMax));
        ImGui.ProgressBar(this.presenter.ActiveModsPercentage / 100f, new Vector2(-1, 0), string.Format(this.loc.GetString("StatusView_ActiveMods"), this.presenter.ActiveModsPercentage.ToString("0.00")));

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        // Hierarchy Section
        ImGui.SetWindowFontScale(1.2f);
        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), this.loc.GetString("StatusView_Hierarchy"));
        ImGui.SetWindowFontScale(1.0f);
        ImGui.Spacing();

        if (this.presenter.ActiveCollections.Count == 0) {
            ImGui.TextDisabled(this.loc.GetString("StatusView_NoActiveCollection"));
        } else {
            foreach (var node in this.presenter.ActiveCollections) {
                Dalamud.Interface.FontAwesomeIcon icon = node.IsInherited ? Dalamud.Interface.FontAwesomeIcon.AngleRight : Dalamud.Interface.FontAwesomeIcon.CaretRight;

                ImGui.PushFont(Dalamud.Interface.UiBuilder.IconFont);
                ImGui.TextUnformatted(icon.ToIconString());
                ImGui.PopFont();

                ImGui.SameLine();
                ImGui.TextUnformatted(node.Name);
            }
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        // Action Buttons
        if (ImGui.Button(this.loc.GetString("StatusView_BtnRefresh"))) {
            this.presenter.RefreshReport();
        }

        ImGui.SameLine();
        if (ImGui.Button(this.loc.GetString("StatusView_BtnManageCache"))) {
            this.presenter.ManageConflictCache();
        }

        ImGui.SameLine();
        if (ImGui.Button(this.loc.GetString("StatusView_BtnViewConflicts"))) {
            this.presenter.ViewConflicts();
        }
    }
}