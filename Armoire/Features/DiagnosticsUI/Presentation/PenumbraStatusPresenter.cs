namespace Armoire.Features.DiagnosticsUI.Presentation;

using Armoire.Features.ConflictEngine;
using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.LocalScanner;
using Armoire.Features.LocalScanner.Models;
using System.Collections.Generic;

public class PenumbraStatusPresenter : IPenumbraStatusPresenter {
    private readonly IPenumbraSyncManager syncManager;
    private readonly IModScannerManager scannerManager;
    private readonly IConflictListPresenter conflictListPresenter;

    // --- UI state variables updated by the sync manager event ---
    public string IntegrationStatus { get; private set; } = "Inactive";
    public string ConnectedCharacter { get; private set; } = "None";
    public bool IsBgScanInProgress => this.scannerManager.State == ScanState.Scanning;

    public int TotalIpcMods { get; private set; } = 0;
    public int ConfiguredMods { get; private set; } = 0;
    public int ConfiguredModsMax { get; private set; } = 0;
    public int EnabledMods { get; private set; } = 0;
    public int EnabledModsMax { get; private set; } = 0;
    public int ConflictingMods { get; private set; } = 0;
    public int ConflictingModsMax { get; private set; } = 0;

    public float GlobalDirectoryPercentage => this.ConfiguredModsMax > 0 ? (float)this.ConfiguredMods / this.ConfiguredModsMax * 100 : 0f;
    public float CollectionPercentage => this.EnabledModsMax > 0 ? (float)this.EnabledMods / this.EnabledModsMax * 100 : 0f;
    public float ActiveModsPercentage => this.ConflictingModsMax > 0 ? (float)this.ConflictingMods / this.ConflictingModsMax * 100 : 0f;

    public IReadOnlyList<CollectionNode> ActiveCollections { get; private set; } = new List<CollectionNode>();

    public PenumbraStatusPresenter(
        IPenumbraSyncManager syncManager,
        IModScannerManager scannerManager,
        IConflictListPresenter conflictListPresenter) {
        this.syncManager = syncManager;
        this.scannerManager = scannerManager;
        this.conflictListPresenter = conflictListPresenter;

        // Hook up to sync manager updates to refresh the UI automatically
        this.syncManager.OnStatusUpdated += UpdateStats;
    }

    public void RefreshReport() {
        this.syncManager.ForceRefresh();
    }

    public void ManageConflictCache() {
        // Logic to open cache manager UI or trigger cache cleanup
    }

    public void ViewConflicts() {
        this.conflictListPresenter.Open();
    }

    private void UpdateStats(PenumbraStatusResult result) {
        // Handle disconnected or inactive states
        if (result == null || !result.IsEnabled) {
            this.IntegrationStatus = "Inactive";
            this.ConnectedCharacter = "None";
            return;
        }

        this.IntegrationStatus = "Active";
        this.ConnectedCharacter = string.IsNullOrEmpty(result.PlayerName) ? "None" : result.PlayerName;

        // Map PenumbraStatusResult properties directly to Presenter states
        this.TotalIpcMods = result.ModCount;

        this.ConfiguredMods = result.CollectionTotalMods;
        this.ConfiguredModsMax = result.ModCount;

        this.EnabledMods = result.CollectionEnabledMods;
        this.EnabledModsMax = result.CollectionTotalMods;

        this.ConflictingMods = result.ConflictModCount;
        this.ConflictingModsMax = result.CollectionEnabledMods;

        // Map string list to structured node list (Index 0 is active, > 0 are inherited)
        var nodes = new List<CollectionNode>();
        if (result.ActiveCollections != null) {
            for (int i = 0; i < result.ActiveCollections.Count; i++) {
                nodes.Add(new CollectionNode {
                    Name = result.ActiveCollections[i],
                    IsInherited = i > 0
                });
            }
        }
        this.ActiveCollections = nodes;
    }
}