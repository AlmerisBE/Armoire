namespace Armoire.Features.Outfits.Presentation;

using Armoire.Features.Outfits.Models;

public interface IOutfitDetailsPresenter {
    /// <summary>
    /// Gets or sets whether the outfit details inspection window is currently visible.
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Gets the outfit currently being inspected. Returns null if no active inspection.
    /// </summary>
    ArmoireOutfit? CurrentOutfit { get; }

    /// <summary>
    /// Opens the inspection window for a specific outfit snapshot.
    /// </summary>
    void Open(ArmoireOutfit outfit);
}