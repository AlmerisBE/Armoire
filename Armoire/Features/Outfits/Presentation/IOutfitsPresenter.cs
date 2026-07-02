namespace Armoire.Features.Outfits.Presentation;

using Armoire.Features.Outfits.Models;
using System;
using System.Collections.Generic;

public interface IOutfitsPresenter {
    /// <summary>
    /// Gets the list of all locally saved outfits.
    /// </summary>
    IReadOnlyList<ArmoireOutfit> Outfits { get; }

    /// <summary>
    /// Captures the current character's modifications and saves them as a new outfit.
    /// </summary>
    void CreateOutfit(string name);

    /// <summary>
    /// Permanently deletes an outfit by its unique identifier.
    /// </summary>
    void DeleteOutfit(Guid id);

    /// <summary>
    /// Re-applies the saved appearance and verifies mod requirements.
    /// </summary>
    void ActivateOutfit(ArmoireOutfit outfit);

    /// <summary>
    /// Evaluates if all required mods for this outfit are currently installed and enabled in Penumbra.
    /// </summary>
    /// <returns>A tuple containing the readiness state and a list of missing mod names.</returns>
    (bool IsReady, List<string> MissingModNames) CheckOutfitReadiness(ArmoireOutfit outfit);

    /// <summary>
    /// Analyzes a list of required mods against the current Penumbra installation and active state.
    /// </summary>
    IReadOnlyList<ModRequirementAnalysis> AnalyzeOutfitRequirements(IEnumerable<OutfitModRequirement> requirements);

    /// <summary>
    /// Exports the outfit to the system clipboard as a Base64 encoded string for sharing.
    /// </summary>
    void ExportToClipboard(ArmoireOutfit outfit);

    /// <summary>
    /// Saves any manual modifications made to an outfit's equipment or required mods.
    /// </summary>
    void UpdateOutfit(ArmoireOutfit outfit);
}