namespace Armoire.Features.Penumbra.Core;

using Armoire.Features.Penumbra.Interfaces;
using Dalamud.Plugin.Services;

public class PenumbraAnalyzer : IPenumbraAnalyzer {
    private readonly IPenumbraClient penumbraClient;
    private readonly IObjectTable objectTable;

    public PenumbraAnalyzer(IPenumbraClient penumbraClient, IObjectTable objectTable) {
        this.penumbraClient = penumbraClient;
        this.objectTable = objectTable;
    }

    public string GetStatusReport() {
        if (!this.penumbraClient.IsEnabled()) {
            return "Penumbra est hors ligne ou non installé.";
        }

        int modCount = this.penumbraClient.GetModsCount();
        var playerName = this.objectTable.LocalPlayer?.Name.TextValue;

        if (string.IsNullOrEmpty(playerName)) {
            return $"Penumbra est connecté. Mods installés : {modCount}\nPersonnage : Aucun (Non connecté)";
        }

        var hierarchy = this.penumbraClient.GetActiveCollectionHierarchy();
        string collectionText = hierarchy.Count > 0
            ? string.Join(" -> ", hierarchy)
            : "Aucune collection active";

        return $"Penumbra est connecté. Mods installés : {modCount}\nPersonnage : {playerName}\nCollections actives : {collectionText}";
    }
}