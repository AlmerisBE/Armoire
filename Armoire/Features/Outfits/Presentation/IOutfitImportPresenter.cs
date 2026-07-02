namespace Armoire.Features.Outfits.Presentation;

using Armoire.Features.Outfits.Models;
using System.Collections.Generic;

public interface IOutfitImportPresenter {
    bool IsVisible { get; set; }
    string ShareCode { get; set; }
    string ErrorMessage { get; }

    ArmoireOutfit? StagedOutfit { get; }
    IReadOnlyList<OutfitModRequirement> MissingMods { get; }

    /// <summary>
    /// Opens the import window and resets all previous states.
    /// </summary>
    void Open();

    /// <summary>
    /// Decodes the Base64/GZip string and analyzes missing mods.
    /// </summary>
    void AnalyzeCode();

    /// <summary>
    /// Saves the staged outfit to the repository and closes the window.
    /// </summary>
    void ConfirmImport();

    /// <summary>
    /// Discards the staged outfit and closes the window.
    /// </summary>
    void CancelImport();
}