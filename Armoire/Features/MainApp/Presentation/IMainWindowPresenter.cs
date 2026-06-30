namespace Armoire.Features.MainApp.Presentation;

using Armoire; // For ModifiedModEntry
using Armoire.Features.ConflictEngine.Models;
using System.Collections.Generic;

public interface IMainWindowPresenter {
    // --- STATE DATA ---
    string ConnectedCharacter { get; }
    int TotalInstalledMods { get; }
    int ConfiguredMods { get; }
    int ActiveMods { get; }
    string MainCollectionName { get; }

    int IgnoredConflictsCount { get; }
    int ResolvedConflictsCount { get; }

    string PenumbraModDirectory { get; }
    string PluginVersion { get; }
    string PluginAuthor { get; }

    // --- CONFLICTS & RESOLUTIONS ---
    IReadOnlyList<PenumbraMod> CurrentConflicts { get; }
    string ConflictSearchQuery { get; set; }
    IReadOnlyDictionary<string, ModifiedModEntry> ResolvedMods { get; }

    // --- ACTIONS ---
    void OpenConflictResolution(string modId);
    void OpenDiscord();

    bool IsModActivelyPatched(string modId);
    void ResetMod(string modId);
    void RestoreMod(string modId, Dictionary<string, string> swaps);
}