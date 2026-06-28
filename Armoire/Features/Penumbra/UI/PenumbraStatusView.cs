namespace Armoire.Features.Penumbra.UI;

using Armoire.Core.UI;
using Dalamud.Bindings.ImGui;
using System.Linq;

public class PenumbraStatusView : IUiComponent {
    private readonly PenumbraStatusPresenter presenter;

    public PenumbraStatusView(PenumbraStatusPresenter presenter) {
        this.presenter = presenter;
    }

    public void Draw() {
        this.presenter.Tick();

        ImGui.TextDisabled("Penumbra Integration Status");
        ImGui.Spacing();

        var status = this.presenter.CurrentStatus;

        if (!status.IsEnabled) {
            ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.3f, 0.3f, 1.0f), "Penumbra est hors ligne ou non installé.");
        } else {
            ImGui.Text($"Total mods installés (Global) : {status.ModCount}");

            if (!status.IsPlayerConnected) {
                ImGui.Text("Personnage : Aucun (Non connecté)");
            } else {
                ImGui.Text($"Personnage actif : {status.PlayerName}");

                string collectionText = status.ActiveCollections.Any()
                    ? string.Join(" -> ", status.ActiveCollections)
                    : "Aucune collection active";

                ImGui.Text($"Hiérarchie des collections : {collectionText}");

                // Affichage des statistiques de la collection active
                ImGui.Text($"Mods de la collection : {status.CollectionEnabledMods} activés / {status.CollectionTotalMods} configurés");
            }
        }

        ImGui.Spacing();

        if (ImGui.Button("Refresh Status")) {
            this.presenter.RefreshReport();
        }
    }
}