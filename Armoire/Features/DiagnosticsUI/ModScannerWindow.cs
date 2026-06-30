namespace Armoire.Features.DiagnosticsUI;

using Armoire.Core.Localization;
using Armoire.Features.LocalScanner.Presentation;
using Dalamud.Bindings.ImGui;
using System.Numerics;

public class ModScannerWindow {
    private readonly ILocalizationService loc;
    private readonly IModScannerPresenter presenter;

    public ModScannerWindow(ILocalizationService loc, IModScannerPresenter presenter) {
        this.loc = loc;
        this.presenter = presenter;
    }

    public void Draw() {
        if (!this.presenter.IsVisible) {
            return;
        }

        bool windowOpen = this.presenter.IsVisible;
        ImGui.SetNextWindowSize(new Vector2(500, 250), ImGuiCond.FirstUseEver);

        if (ImGui.Begin(this.loc.GetString("Scanner_WindowTitle"), ref windowOpen, ImGuiWindowFlags.NoCollapse)) {
            // If the user clicks the red X, we cancel the scan to be safe
            if (!windowOpen) {
                this.presenter.CancelScan();
                return;
            }

            ImGui.TextWrapped(this.loc.GetString("Scanner_RulesText"));
            ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

            // Progress tracking
            ImGui.TextUnformatted(string.Format(this.loc.GetString("Scanner_Progress"), this.presenter.ScannedCount, this.presenter.TotalCount));

            float fraction = this.presenter.TotalCount > 0
                ? (float)this.presenter.ScannedCount / this.presenter.TotalCount
                : 0f;

            ImGui.ProgressBar(fraction, new Vector2(-1, 24), $"{fraction * 100:0.0}%");
            ImGui.Spacing();

            if (this.presenter.IgnoredErrorsCount > 0) {
                ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.0f, 1.0f), string.Format(this.loc.GetString("Scanner_IgnoredErrors"), this.presenter.IgnoredErrorsCount));
                ImGui.Spacing();
            }

            ImGui.Separator(); ImGui.Spacing();

            // Action Buttons
            if (!this.presenter.IsScanning) {
                if (ImGui.Button(this.loc.GetString("Scanner_BtnStart"))) {
                    this.presenter.StartScan();
                }
            } else {
                if (this.presenter.IsPaused) {
                    if (ImGui.Button(this.loc.GetString("Scanner_BtnResume"))) {
                        this.presenter.ResumeScan();
                    }
                } else {
                    if (ImGui.Button(this.loc.GetString("Scanner_BtnPause"))) {
                        this.presenter.PauseScan();
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button(this.loc.GetString("Scanner_BtnBackground"))) {
                    this.presenter.CloseToBackground();
                }

                ImGui.SameLine();
                if (ImGui.Button(this.loc.GetString("Scanner_BtnCancel"))) {
                    this.presenter.CancelScan();
                }
            }
        }
        ImGui.End();
    }
}