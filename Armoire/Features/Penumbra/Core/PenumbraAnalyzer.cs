namespace Armoire.Features.Penumbra.Core;

using Armoire.Features.Penumbra.Core.Models;
using Armoire.Features.Penumbra.Interfaces;
using Dalamud.Plugin.Services;

public class PenumbraAnalyzer : IPenumbraAnalyzer {
    private readonly IPenumbraClient penumbraClient;
    private readonly IObjectTable objectTable;

    public PenumbraAnalyzer(IPenumbraClient penumbraClient, IObjectTable objectTable) {
        this.penumbraClient = penumbraClient;
        this.objectTable = objectTable;
    }

    public PenumbraStatusResult GetStatusReport() {
        var result = new PenumbraStatusResult();

        if (!this.penumbraClient.IsEnabled()) {
            result.IsEnabled = false;
            return result;
        }

        result.IsEnabled = true;
        result.ModCount = this.penumbraClient.GetModsCount();
        result.PlayerName = this.objectTable.LocalPlayer?.Name.TextValue ?? string.Empty;

        if (result.IsPlayerConnected) {
            var (hierarchy, totalMods, enabledMods) = this.penumbraClient.GetActiveCollectionDetails();
            result.ActiveCollections = hierarchy;
            result.CollectionTotalMods = totalMods;
            result.CollectionEnabledMods = enabledMods;
        }

        return result;
    }
}