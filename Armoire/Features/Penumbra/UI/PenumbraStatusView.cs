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
        ImGui.Text($"Nombre de mods détectés par l'IPC : {status.ModCount}");
        ImGui.Text($"Collection assignée au joueur : {status.PlayerName}");
        ImGui.Text($"Mods en conflit (surchargés) : {status.ConflictModCount}");

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