namespace Armoire.Features.DiagnosticsUI;

using Armoire.Core.Localization;
using Armoire.Core.UI;
using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.ModDetails.UI;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

public class PenumbraStatusView : IUiComponent {
    private readonly PenumbraStatusPresenter presenter;
    private readonly ModScannerWindow modScannerWindow;
    private readonly ConflictListWindow conflictListWindow;
    private readonly ModDetailsWindow modDetailsWindow;
    private readonly ILocalizationService loc;

    public PenumbraStatusView(
        PenumbraStatusPresenter presenter,
        ModScannerWindow modScannerWindow,
        ConflictListWindow conflictListWindow,
        ModDetailsWindow modDetailsWindow,
        ILocalizationService localizationService) {

        this.presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        this.modScannerWindow = modScannerWindow ?? throw new ArgumentNullException(nameof(modScannerWindow));
        this.conflictListWindow = conflictListWindow ?? throw new ArgumentNullException(nameof(conflictListWindow));
        this.modDetailsWindow = modDetailsWindow ?? throw new ArgumentNullException(nameof(modDetailsWindow));
        this.loc = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
    }

    public void Draw() {
        var status = this.presenter.CurrentStatus;

        if (this.modScannerWindow.IsScanning()) {
            DrawBackgroundScanIndicator();
        }

        DrawBaselineMetadata(status);
        DrawSeparator();
        DrawStatisticsFunnel(status);
        DrawSeparator();
        DrawCollectionHierarchy(status);
        DrawSeparator();
        DrawActionButtons(status, this.modScannerWindow.IsScanning());

        this.modScannerWindow.Draw();
        this.conflictListWindow.Draw(status.ConflictingMods);
        this.modDetailsWindow.Draw(status.GlobalState);
    }

    private void DrawBaselineMetadata(PenumbraStatusResult status) {
        string statusText = status.IsEnabled ? this.loc.GetString("StatusView_Active") : this.loc.GetString("StatusView_Inactive");
        string playerText = string.IsNullOrEmpty(status.PlayerName) ? this.loc.GetString("StatusView_None") : status.PlayerName;

        ImGui.Text(string.Format(this.loc.GetString("StatusView_IntegrationStatus"), statusText));
        ImGui.Text(string.Format(this.loc.GetString("StatusView_ConnectedCharacter"), playerText));
    }

    private void DrawStatisticsFunnel(PenumbraStatusResult status) {
        ImGui.Text(this.loc.GetString("StatusView_ModDistribution"));
        ImGui.Text(string.Format(this.loc.GetString("StatusView_TotalIpcMods"), status.ModCount));
        ImGui.Spacing();

        float collectionRatio = status.ModCount > 0 ? (float)status.CollectionTotalMods / status.ModCount : 0f;
        ImGui.Text(string.Format(this.loc.GetString("StatusView_ConfiguredMods"), status.CollectionTotalMods, status.ModCount));
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.2f, 0.6f, 1.0f, 1.0f));
        ImGui.ProgressBar(collectionRatio, new Vector2(-1, 16), string.Format(this.loc.GetString("StatusView_GlobalDirectory"), (collectionRatio * 100)));
        ImGui.PopStyleColor();

        ImGui.Spacing();

        float enabledRatio = status.CollectionTotalMods > 0 ? (float)status.CollectionEnabledMods / status.CollectionTotalMods : 0f;
        ImGui.Text(string.Format(this.loc.GetString("StatusView_EnabledMods"), status.CollectionEnabledMods, status.CollectionTotalMods));
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.2f, 0.8f, 0.2f, 1.0f));
        ImGui.ProgressBar(enabledRatio, new Vector2(-1, 16), string.Format(this.loc.GetString("StatusView_Collection"), (enabledRatio * 100)));
        ImGui.PopStyleColor();

        ImGui.Spacing();

        float conflictRatio = status.CollectionEnabledMods > 0 ? (float)status.ConflictModCount / status.CollectionEnabledMods : 0f;
        ImGui.Text(string.Format(this.loc.GetString("StatusView_ConflictingMods"), status.ConflictModCount, status.CollectionEnabledMods));
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(1.0f, 0.4f, 0.0f, 1.0f));
        ImGui.ProgressBar(conflictRatio, new Vector2(-1, 16), string.Format(this.loc.GetString("StatusView_ActiveMods"), (conflictRatio * 100)));
        ImGui.PopStyleColor();
    }

    private void DrawCollectionHierarchy(PenumbraStatusResult status) {
        ImGui.Text(this.loc.GetString("StatusView_Hierarchy"));
        if (status.ActiveCollections == null || status.ActiveCollections.Count == 0) {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), this.loc.GetString("StatusView_NoActiveCollection"));
            return;
        }

        int visualIndex = 0;
        for (int i = status.ActiveCollections.Count - 1; i >= 0; i--) {
            string prefix = visualIndex == 0 ? this.loc.GetString("StatusView_ActivePrefix") : this.loc.GetString("StatusView_InheritedPrefix");
            ImGui.Text($"{prefix}{status.ActiveCollections[i]}");
            visualIndex++;
        }
    }

    private void DrawActionButtons(PenumbraStatusResult status, bool isScanning) {
        if (isScanning) {
            ImGui.BeginDisabled();
        }

        if (ImGui.Button(this.loc.GetString("StatusView_BtnRefresh"))) {
            this.presenter.RefreshReport();
        }

        ImGui.SameLine();

        if (ImGui.Button(this.loc.GetString("StatusView_BtnManageCache"))) {
            this.modScannerWindow.Open(status.ModCount);
        }

        if (status.ConflictModCount > 0) {
            ImGui.SameLine();
            if (ImGui.Button(this.loc.GetString("StatusView_BtnViewConflicts"))) {
                this.conflictListWindow.Open();
            }
        }

        if (isScanning) {
            ImGui.EndDisabled();
        }
    }

    private void DrawBackgroundScanIndicator() {
        ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), this.loc.GetString("StatusView_BgScanInProgress"));
        float progress = this.modScannerWindow.GetScanProgress();
        ImGui.ProgressBar(progress, new Vector2(-1, 14), $"{(progress * 100):0.0}%");
        DrawSeparator();
    }

    private void DrawSeparator() {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }
}