namespace Armoire.Features.Outfits.Presentation;

using Armoire.Features.Outfits.Models;

public class OutfitDetailsPresenter : IOutfitDetailsPresenter {
    public bool IsVisible { get; set; } = false;
    public ArmoireOutfit? CurrentOutfit { get; private set; }

    public void Open(ArmoireOutfit outfit) {
        this.CurrentOutfit = outfit;
        this.IsVisible = true;
    }
}