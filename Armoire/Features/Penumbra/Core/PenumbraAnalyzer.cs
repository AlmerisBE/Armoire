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

        var collectionName = this.penumbraClient.GetCollectionForCharacter(playerName) ?? "Collection par défaut";

        return $"Penumbra est connecté. Mods installés : {modCount}\nPersonnage : {playerName}\nCollection active : {collectionName}";
    }
}