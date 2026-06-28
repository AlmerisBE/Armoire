namespace Armoire.Features.Penumbra.Core;

using Armoire.Features.Penumbra.Core.Models;
using Armoire.Features.Penumbra.Interfaces;
using Dalamud.Plugin.Services;
using System.Linq;

public class PenumbraAnalyzer : IPenumbraAnalyzer {
    private readonly IPenumbraClient penumbraClient;
    private readonly IPenumbraRepository penumbraRepository;
    private readonly IObjectTable objectTable;

    public PenumbraAnalyzer(IPenumbraClient penumbraClient, IPenumbraRepository penumbraRepository, IObjectTable objectTable) {
        this.penumbraClient = penumbraClient;
        this.penumbraRepository = penumbraRepository;
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
            var activeCollection = this.penumbraClient.GetActiveCollection();

            // On demande l'état effectif au Repository en mémoire !
            var effectiveState = this.penumbraRepository.ComputeEffectiveState(activeCollection.Id.ToString());

            result.ActiveCollections = effectiveState.HierarchyNames;
            result.CollectionTotalMods = effectiveState.EffectiveMods.Count;

            // On compte uniquement les mods dont IsEnabled est à "true"
            result.CollectionEnabledMods = effectiveState.EffectiveMods.Values.Count(m => m.IsEnabled);

            // Fallback si le repository n'a pas encore eu le temps de charger
            if (result.ActiveCollections.Count == 0 && !string.IsNullOrEmpty(activeCollection.Name)) {
                result.ActiveCollections.Add(activeCollection.Name);
            }
        }

        return result;
    }
}