namespace Armoire.Features.DiagnosticsUI.Presentation;

using Armoire.Features.ConflictEngine.Models;
using System.Collections.Generic;

public interface IConflictListPresenter {
    // --- STATE DATA ---
    bool IsVisible { get; set; }
    string SearchQuery { get; set; }

    /// <summary>
    /// Gets the mods modified by Armoire, dynamically filtered by the search query.
    /// </summary>
    IReadOnlyDictionary<string, Armoire.Features.LocalScanner.Models.ModifiedModEntry> FilteredModifiedMods { get; }

    /// <summary>
    /// Gets the vanilla conflicts, dynamically filtered by the search query and excluding already modified mods.
    /// </summary>
    IReadOnlyList<PenumbraMod> FilteredConflicts { get; }

    // --- ACTIONS ---
    void Open();
    void UpdateLiveConflicts(List<PenumbraMod> liveConflicts);
    void OpenModDetails(string modId);
    bool IsModActivelyPatched(string modId);
    void ResetMod(string modId);
    void RestoreMod(string modId, Dictionary<string, string> swaps);
}