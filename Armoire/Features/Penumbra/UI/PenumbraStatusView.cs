namespace Armoire.Features.Penumbra.UI;

using Armoire.Core.UI;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

public class PenumbraStatusView : IUiComponent {
    private readonly PenumbraStatusPresenter presenter;
    private readonly ModScannerWindow modScannerWindow;

    public PenumbraStatusView(PenumbraStatusPresenter presenter, ModScannerWindow modScannerWindow) {
        this.presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        this.modScannerWindow = modScannerWindow ?? throw new ArgumentNullException(nameof(modScannerWindow));
    }

    public void Draw() {
        var status = this.presenter.CurrentStatus;

        // Render baseline metadata
        ImGui.Text($"Statut de l'intégration : {(status.IsEnabled ? "Actif" : "Inactif")}");
        ImGui.Text($"Personnage connecté : {(string.IsNullOrEmpty(status.PlayerName) ? "Aucun" : status.PlayerName)}");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Analyse de la répartition des mods :");
        ImGui.Spacing();

        // Tier 1: Configured mods inside the active collection lineage relative to overall installed mods (IPC)
        float collectionRatio = status.ModCount > 0 ? (float)status.CollectionTotalMods / status.ModCount : 0f;
        ImGui.Text($"Mods configurés dans vos collections : {status.CollectionTotalMods} / {status.ModCount}");
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.2f, 0.6f, 1.0f, 1.0f)); // Professional Blue
        ImGui.ProgressBar(collectionRatio, new Vector2(-1, 16), $"{(collectionRatio * 100):0.0}% du répertoire global");
        ImGui.PopStyleColor();

        ImGui.Spacing();

        // Tier 2: Enabled mods relative to the total number of mods assigned to the active collection
        float enabledRatio = status.CollectionTotalMods > 0 ? (float)status.CollectionEnabledMods / status.CollectionTotalMods : 0f;
        ImGui.Text($"Mods actuellement activés : {status.CollectionEnabledMods} / {status.CollectionTotalMods}");
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.2f, 0.8f, 0.2f, 1.0f)); // Safe Green
        ImGui.ProgressBar(enabledRatio, new Vector2(-1, 16), $"{(enabledRatio * 100):0.0}% de la collection");
        ImGui.PopStyleColor();

        ImGui.Spacing();

        // Tier 3: Enabled/Active mods that are experiencing conflicts or overrides
        float conflictRatio = status.CollectionEnabledMods > 0 ? (float)status.ConflictModCount / status.CollectionEnabledMods : 0f;
        ImGui.Text($"Mods actifs souffrant de conflits (surchargés) : {status.ConflictModCount} / {status.CollectionEnabledMods}");
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(1.0f, 0.4f, 0.0f, 1.0f)); // Alert Orange
        ImGui.ProgressBar(conflictRatio, new Vector2(-1, 16), $"{(conflictRatio * 100):0.0}% des mods actifs");
        ImGui.PopStyleColor();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Hiérarchie des collections actives :");
        if (status.ActiveCollections == null || status.ActiveCollections.Count == 0) {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "  Aucune collection active détectée.");
        } else {
            int visualIndex = 0;
            // Iterate backwards to display the active root collection first
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