namespace Armoire.Features.DiagnosticsUI.Presentation;

using System.Collections.Generic;

/// <summary>
/// Represents a node in the active collection hierarchy.
/// </summary>
public class CollectionNode {
    public string Name { get; set; } = string.Empty;
    public bool IsInherited { get; set; }
}

public interface IPenumbraStatusPresenter {
    // --- STATE DATA ---
    string IntegrationStatus { get; }
    string ConnectedCharacter { get; }
    bool IsBgScanInProgress { get; }

    // Statistics for the distribution bars/texts
    int TotalIpcMods { get; }
    int ConfiguredMods { get; }
    int ConfiguredModsMax { get; }
    int EnabledMods { get; }
    int EnabledModsMax { get; }
    int ConflictingMods { get; }
    int ConflictingModsMax { get; }

    float GlobalDirectoryPercentage { get; }
    float CollectionPercentage { get; }
    float ActiveModsPercentage { get; }

    IReadOnlyList<CollectionNode> ActiveCollections { get; }

    // --- ACTIONS ---
    void RefreshReport();
    void ManageConflictCache();
    void ViewConflicts();
}