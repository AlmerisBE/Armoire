namespace Armoire.Features.Penumbra.Core;

using Armoire.Features.Penumbra.Core.Models;
using Armoire.Features.Penumbra.Interfaces;
using Dalamud.Plugin.Services;
using System;
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
            // L'appel au wrapper officiel
            var activeCollection = this.penumbraClient.GetActiveCollection();
            string targetId = activeCollection.Id.ToString();

            // Résolution de secours (au cas où Penumbra ne renverrait qu'un nom avec Guid.Empty)
            if (activeCollection.Id == Guid.Empty && !string.IsNullOrEmpty(activeCollection.Name)) {
                var matched = this.penumbraRepository.GetAllCollections().FirstOrDefault(c => c.Name == activeCollection.Name);
                if (matched != null) {
                    targetId = matched.Id;
                }
            }

            var effectiveState = this.penumbraRepository.ComputeEffectiveState(targetId);

            result.ActiveCollections = effectiveState.HierarchyNames;
            result.CollectionTotalMods = effectiveState.EffectiveMods.Count;
            result.CollectionEnabledMods = effectiveState.EffectiveMods.Values.Count(m => m.IsEnabled);
            result.ConflictModCount = effectiveState.ConflictModCount;
            result.ConflictingMods = effectiveState.ConflictingMods;

            if (result.ActiveCollections.Count == 0 && !string.IsNullOrEmpty(activeCollection.Name)) {
                result.ActiveCollections.Add(activeCollection.Name);
            }
        }

        return result;
    }
}