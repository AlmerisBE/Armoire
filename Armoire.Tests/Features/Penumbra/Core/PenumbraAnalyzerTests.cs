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
    public void GetStatusReport_QuandPenumbraEstDesactive_RetourneMessageHorsLigne() {
        // Arrange
        var fauxClient = Substitute.For<IPenumbraClient>();
        fauxClient.IsEnabled().Returns(false);
        var fauxObjectTable = Substitute.For<IObjectTable>();

        var analyseur = new PenumbraAnalyzer(fauxClient, fauxObjectTable);

        // Act
        var resultat = analyseur.GetStatusReport();

        // Assert
        Assert.Equal("Penumbra est hors ligne ou non installé.", resultat);
    }

    [Fact]
    public void GetStatusReport_QuandPersonnageEstConnecte_RetourneStatistiquesCompletes() {
        // Arrange
        var fauxClient = Substitute.For<IPenumbraClient>();
        fauxClient.IsEnabled().Returns(true);
        fauxClient.GetModsCount().Returns(42);
        fauxClient.GetCollectionForCharacter("Almeris Test").Returns("Ma Collection Active");

        var fauxObjectTable = Substitute.For<IObjectTable>();
        var fauxJoueur = Substitute.For<IPlayerCharacter>();

        fauxJoueur.Name.Returns((SeString)"Almeris Test");
        fauxObjectTable.LocalPlayer.Returns(fauxJoueur);

        var analyseur = new PenumbraAnalyzer(fauxClient, fauxObjectTable);

        // Act
        var resultat = analyseur.GetStatusReport();

        // Assert
        var resultatAttendu = "Penumbra est connecté. Mods installés : 42\nPersonnage : Almeris Test\nCollection active : Ma Collection Active";
        Assert.Equal(resultatAttendu, resultat);
    }
}