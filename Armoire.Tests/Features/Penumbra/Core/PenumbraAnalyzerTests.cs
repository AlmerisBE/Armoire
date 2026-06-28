namespace Armoire.Tests.Features.Penumbra.Core;

using Armoire.Features.Penumbra.Core;
using Armoire.Features.Penumbra.Core.Models;
using Armoire.Features.Penumbra.Interfaces;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using NSubstitute;
using Xunit;

public class PenumbraAnalyzerTests {
    [Fact]
    public void GetStatusReport_QuandPenumbraEstDesactive_RetourneResultatInactif() {
        // Arrange
        var fauxClient = Substitute.For<IPenumbraClient>();
        fauxClient.IsEnabled().Returns(false);
        var fauxRepository = Substitute.For<IPenumbraRepository>();
        var fauxObjectTable = Substitute.For<IObjectTable>();

        var analyseur = new PenumbraAnalyzer(fauxClient, fauxRepository, fauxObjectTable);

        // Act
        var resultat = analyseur.GetStatusReport();

        // Assert
        Assert.False(resultat.IsEnabled);
    }

    [Fact]
    public void GetStatusReport_QuandPersonnageEstConnecte_RetourneDonneesStructurees() {
        // Arrange
        var fauxClient = Substitute.For<IPenumbraClient>();
        fauxClient.IsEnabled().Returns(true);
        fauxClient.GetModsCount().Returns(42);
        fauxClient.GetActiveCollection(Arg.Any<string>()).Returns((Guid.NewGuid(), "Ysaline Sylv'anir"));

        var fauxRepo = Substitute.For<IPenumbraRepository>();
        var etatSimule = new EffectiveCollectionState();
        etatSimule.HierarchyNames.Add("Ysaline Sylv'anir");
        etatSimule.HierarchyNames.Add("Default");
        etatSimule.EffectiveMods["Mod1"] = new Armoire.Features.Penumbra.Core.Domain.PenumbraMod { IsEnabled = true };
        etatSimule.EffectiveMods["Mod2"] = new Armoire.Features.Penumbra.Core.Domain.PenumbraMod { IsEnabled = false };

        fauxRepo.ComputeEffectiveState(Arg.Any<string>()).Returns(etatSimule);

        var fauxObjectTable = Substitute.For<IObjectTable>();
        var fauxJoueur = Substitute.For<IPlayerCharacter>();
        fauxJoueur.Name.Returns((Dalamud.Game.Text.SeStringHandling.SeString)"Almeris Test");
        fauxObjectTable.LocalPlayer.Returns(fauxJoueur);

        var analyseur = new PenumbraAnalyzer(fauxClient, fauxRepo, fauxObjectTable);

        // Act
        var resultat = analyseur.GetStatusReport();

        // Assert
        Assert.True(resultat.IsEnabled);
        Assert.Equal(42, resultat.ModCount);
        Assert.Equal(2, resultat.CollectionTotalMods);
        Assert.Equal(1, resultat.CollectionEnabledMods); // Seulement le "Mod1" est actif
        Assert.Equal("Almeris Test", resultat.PlayerName);
        Assert.Equal(etatSimule.HierarchyNames, resultat.ActiveCollections);
    }
}