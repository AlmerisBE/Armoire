namespace Armoire.Tests.Features.Penumbra.Core;

using Armoire.Features.Penumbra.Core;
using Armoire.Features.Penumbra.Interfaces;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using NSubstitute;
using Xunit;

public class PenumbraAnalyzerTests {
    [Fact]
    public void GetStatusReport_QuandPenumbraEstDesactive_RetourneResultatInactif() {
        // Arrange
        var fauxClient = Substitute.For<IPenumbraClient>();
        fauxClient.IsEnabled().Returns(false);
        var fauxObjectTable = Substitute.For<IObjectTable>();

        var analyseur = new PenumbraAnalyzer(fauxClient, fauxObjectTable);

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

        var hierarchieSimulee = new List<string> { "Ysaline Sylv'anir", "Default" };
        fauxClient.GetActiveCollectionHierarchy().Returns(hierarchieSimulee);

        var fauxObjectTable = Substitute.For<IObjectTable>();
        var fauxJoueur = Substitute.For<IPlayerCharacter>();

        fauxJoueur.Name.Returns((SeString)"Almeris Test");
        fauxObjectTable.LocalPlayer.Returns(fauxJoueur);

        var analyseur = new PenumbraAnalyzer(fauxClient, fauxObjectTable);

        // Act
        var resultat = analyseur.GetStatusReport();

        // Assert
        Assert.True(resultat.IsEnabled);
        Assert.Equal(42, resultat.ModCount);
        Assert.Equal("Almeris Test", resultat.PlayerName);
        Assert.True(resultat.IsPlayerConnected);
        Assert.Equal(hierarchieSimulee, resultat.ActiveCollections);
    }
}