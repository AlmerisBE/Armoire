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

        // On simule le retour de la nouvelle hiérarchie sous forme de liste
        var hierarchieSimulee = new List<string> { "Ysaline Sylv'anir", "My Character", "Default" };
        fauxClient.GetActiveCollectionHierarchy().Returns(hierarchieSimulee);

        var fauxObjectTable = Substitute.For<IObjectTable>();
        var fauxJoueur = Substitute.For<IPlayerCharacter>();

        fauxJoueur.Name.Returns((SeString)"Almeris Test");
        fauxObjectTable.LocalPlayer.Returns(fauxJoueur);

        var analyseur = new PenumbraAnalyzer(fauxClient, fauxObjectTable);

        // Act
        var resultat = analyseur.GetStatusReport();

        // Assert
        // L'analyseur joint maintenant les collections avec " -> "
        var resultatAttendu = "Penumbra est connecté. Mods installés : 42\nPersonnage : Almeris Test\nCollections actives : Ysaline Sylv'anir -> My Character -> Default";
        Assert.Equal(resultatAttendu, resultat);
    }
}