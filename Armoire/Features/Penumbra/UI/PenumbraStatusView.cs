namespace Armoire.Features.Penumbra.UI;

using Armoire.Core.UI; // Contains IUiComponent
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

        // Restoring the display of active collections and their inheritance hierarchy
        ImGui.Text("Hiérarchie des collections actives :");
        if (status.ActiveCollections == null || status.ActiveCollections.Count == 0) {
            ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1f), "  Aucune collection active détectée.");
        } else {
            for (int i = 0; i < status.ActiveCollections.Count; i++) {
                // Formatting to show hierarchy lineage (e.g., [Active] -> [Parent] -> [Base])
                string prefix = i == 0 ? "  [Active] " : "  └── [Hérité] ";
                ImGui.Text($"{prefix}{status.ActiveCollections[i]}");
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
            this.modScannerWindow.IsVisible = true;
        }

        this.modScannerWindow.Draw();
    }
}