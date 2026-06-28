using Armoire.Features.Penumbra.Core;
using Armoire.Features.Penumbra.Interfaces;
using NSubstitute;
using Xunit;

namespace Armoire.Tests.Features.Penumbra.Core;

public class PenumbraAnalyzerTests {
    [Fact]
    public void GetStatusReport_QuandPenumbraEstDesactive_RetourneMessageHorsLigne() {
        // Arrange : On crée un faux client qui dit que Penumbra est éteint
        var fauxClient = Substitute.For<IPenumbraClient>();
        fauxClient.IsEnabled().Returns(false);

        var analyseur = new PenumbraAnalyzer(fauxClient);

        // Act
        var resultat = analyseur.GetStatusReport();

        // Assert
        Assert.Equal("Penumbra est hors ligne ou non installé.", resultat);

        // Vérification bonus : on s'assure que l'analyseur n'a pas essayé de compter les mods pour rien
        fauxClient.DidNotReceive().GetModsCount();
    }

    [Fact]
    public void GetStatusReport_QuandPenumbraEstActive_RetourneNombreDeMods() {
        // Arrange : On simule un Penumbra allumé avec 42 mods installés
        var fauxClient = Substitute.For<IPenumbraClient>();
        fauxClient.IsEnabled().Returns(true);
        fauxClient.GetModsCount().Returns(42);

        var analyseur = new PenumbraAnalyzer(fauxClient);

        // Act
        var resultat = analyseur.GetStatusReport();

        // Assert
        Assert.Equal("Penumbra est connecté. Mods installés : 42", resultat);
    }
}
