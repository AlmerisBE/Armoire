namespace Armoire.Features.Penumbra.UI;

using Armoire.Features.Penumbra.Core.Models;
using Armoire.Features.Penumbra.Interfaces;
using Dalamud.Bindings.ImGui;
using System.Numerics;

public class ModScannerWindow {
    private readonly IModScannerManager scannerManager;
    public bool IsVisible { get; set; } = false;

    public ModScannerWindow(IModScannerManager scannerManager) {
        this.scannerManager = scannerManager;
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

        if (ImGui.Begin("Analyse des mods Penumbra", ref windowOpen, ImGuiWindowFlags.NoCollapse)) {
            if (!windowOpen) {
                this.scannerManager.CancelScan();
                this.IsVisible = false;
            }

            ImGui.TextWrapped("Règles du scan : La fermeture de cette fenêtre via la croix rouge ANNULERA le scan en cours. " +
                              "Si vous souhaitez continuer à jouer pendant le scan, utilisez le bouton 'Continuer en arrière-plan'.");
            ImGui.Separator();

            int total = this.scannerManager.TotalMods;
            int processed = this.scannerManager.ProcessedMods;
            float progress = total > 0 ? (float)processed / total : 0f;

            ImGui.Text($"Progression : {processed} / {total} mods scannés");
            ImGui.ProgressBar(progress, new Vector2(-1, 20), $"{(progress * 100):0.0}%");

            if (this.scannerManager.ErrorCount > 0) {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Erreurs ignorées : {this.scannerManager.ErrorCount}");
            }

            ImGui.Spacing();

            if (this.scannerManager.State == ScanState.Idle) {
                if (ImGui.Button("Démarrer le Scan")) {
                    _ = this.scannerManager.StartScanAsync();
                }
            } else if (this.scannerManager.State == ScanState.Scanning) {
                if (ImGui.Button("Mettre en Pause")) {
                    this.scannerManager.PauseScan();
                }
            } else if (this.scannerManager.State == ScanState.Paused) {
                if (ImGui.Button("Reprendre le Scan")) {
                    this.scannerManager.ResumeScan();
                }
            }

            ImGui.SameLine();

            if (this.scannerManager.State != ScanState.Idle) {
                if (ImGui.Button("Annuler")) {
                    this.scannerManager.CancelScan();
                    this.IsVisible = false;
                }
                ImGui.SameLine();
                if (ImGui.Button("Continuer en arrière-plan")) {
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