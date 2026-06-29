namespace Armoire.Features.Penumbra.UI;

using Armoire.Core.UI;
using Dalamud.Bindings.ImGui;
using System;

public class PenumbraStatusView : IUiComponent {
    private readonly PenumbraStatusPresenter presenter;
    private readonly ModScannerWindow modScannerWindow;

    public PenumbraStatusView(PenumbraStatusPresenter presenter, ModScannerWindow modScannerWindow) {
        this.presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        this.modScannerWindow = modScannerWindow ?? throw new ArgumentNullException(nameof(modScannerWindow));
    }

    public void Draw() {
        var status = this.presenter.CurrentStatus;

        ImGui.Text($"Statut de l'intégration : {(status.IsEnabled ? "Actif" : "Inactif")}");
        ImGui.Text($"Personnage connecté : {(string.IsNullOrEmpty(status.PlayerName) ? "Aucun" : status.PlayerName)}");
        ImGui.Text($"Nombre de mods détectés par l'IPC : {status.ModCount}");

        ImGui.Text($"Mods actifs en conflit (surchargés) : {status.ConflictModCount}");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Hiérarchie des collections actives :");
        if (status.ActiveCollections == null || status.ActiveCollections.Count == 0) {
            ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1f), "  Aucune collection active détectée.");
        } else {
            int visualIndex = 0;
            for (int i = status.ActiveCollections.Count - 1; i >= 0; i--) {
                string prefix = visualIndex == 0 ? "  [Active] " : "  └── [Hérité] ";
                ImGui.Text($"{prefix}{status.ActiveCollections[i]}");
                visualIndex++;
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Rafraîchir le rapport global")) {
            this.presenter.RefreshReport();
        }

        ImGui.SameLine();

        if (ImGui.Button("Gérer le cache de conflits")) {
            this.modScannerWindow.Open(status.ModCount);
        }

        this.modScannerWindow.Draw();
    }
}