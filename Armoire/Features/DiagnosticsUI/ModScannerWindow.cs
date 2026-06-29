namespace Armoire.Features.DiagnosticsUI;

using Armoire.Core.Localization;
using Armoire.Features.LocalScanner;
using Armoire.Features.LocalScanner.Models;
using Dalamud.Bindings.ImGui;
using System.Numerics;

public class ModScannerWindow {
    private readonly IModScannerManager scannerManager;
    private readonly ILocalizationService loc;
    public bool IsVisible { get; set; } = false;

    public ModScannerWindow(IModScannerManager scannerManager, ILocalizationService localizationService) {
        this.scannerManager = scannerManager;
        this.loc = localizationService;
    }

    public void Open(int ipcModCount) {
        this.scannerManager.InitializeScanProgress(ipcModCount);
        this.IsVisible = true;
    }

    public void Draw() {
        if (!this.IsVisible) {
            return;
        }

        if (this.scannerManager.State == ScanState.Idle &&
            this.scannerManager.TotalMods > 0 &&
            this.scannerManager.ProcessedMods >= this.scannerManager.TotalMods) {
            this.IsVisible = false;
            return;
        }

        bool windowOpen = this.IsVisible;
        ImGui.SetNextWindowSize(new Vector2(450, 220), ImGuiCond.FirstUseEver);

        if (ImGui.Begin(this.loc.GetString("Scanner_WindowTitle"), ref windowOpen, ImGuiWindowFlags.NoCollapse)) {
            if (!windowOpen) {
                this.scannerManager.CancelScan();
                this.IsVisible = false;
            }

            ImGui.TextWrapped(this.loc.GetString("Scanner_RulesText"));
            ImGui.Separator();

            int total = this.scannerManager.TotalMods;
            int processed = this.scannerManager.ProcessedMods;
            float progress = total > 0 ? (float)processed / total : 0f;

            ImGui.Text(string.Format(this.loc.GetString("Scanner_Progress"), processed, total));
            ImGui.ProgressBar(progress, new Vector2(-1, 20), $"{(progress * 100):0.0}%");

            if (this.scannerManager.ErrorCount > 0) {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), string.Format(this.loc.GetString("Scanner_IgnoredErrors"), this.scannerManager.ErrorCount));
            }

            ImGui.Spacing();

            if (this.scannerManager.State == ScanState.Idle) {
                if (ImGui.Button(this.loc.GetString("Scanner_BtnStart"))) {
                    _ = this.scannerManager.StartScanAsync();
                }
            } else if (this.scannerManager.State == ScanState.Scanning) {
                if (ImGui.Button(this.loc.GetString("Scanner_BtnPause"))) {
                    this.scannerManager.PauseScan();
                }
            } else if (this.scannerManager.State == ScanState.Paused) {
                if (ImGui.Button(this.loc.GetString("Scanner_BtnResume"))) {
                    this.scannerManager.ResumeScan();
                }
            }

            ImGui.SameLine();

            if (this.scannerManager.State != ScanState.Idle) {
                if (ImGui.Button(this.loc.GetString("Scanner_BtnCancel"))) {
                    this.scannerManager.CancelScan();
                    this.IsVisible = false;
                }
                ImGui.SameLine();
                if (ImGui.Button(this.loc.GetString("Scanner_BtnBackground"))) {
                    this.IsVisible = false;
                }
            }
        }
        ImGui.End();
    }

    public float GetScanProgress() {
        int total = this.scannerManager.TotalMods;
        int processed = this.scannerManager.ProcessedMods;
        return total > 0 ? (float)processed / total : 0f;
    }

    public bool IsScanning() {
        return this.scannerManager.State == ScanState.Scanning;
    }
}