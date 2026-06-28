namespace Armoire.Features.Penumbra.Tests;

using Armoire.Features.Penumbra.Core.Models;
using Armoire.Features.Penumbra.Interfaces;
using Armoire.Features.Penumbra.UI;
using Dalamud.Plugin.Services;
using NSubstitute;
using Xunit;

public class PenumbraStatusPresenterTests {
    [Fact]
    public void RefreshReport_ShouldUpdateCurrentStatusFromAnalyzer() {
        // Arrange
        var mockAnalyzer = Substitute.For<IPenumbraAnalyzer>();
        var mockClient = Substitute.For<IPenumbraClient>();
        var mockObjectTable = Substitute.For<IObjectTable>();

        // On prépare l'objet de réponse simulé
        var expectedStatus = new PenumbraStatusResult {
            IsEnabled = true,
            ModCount = 42,
            PlayerName = "Almeris Test",
            ActiveCollections = new List<string> { "Ysaline Sylv'anir", "Default" }
        };

        // On configure le mock pour renvoyer le modèle au lieu de la string
        mockAnalyzer.GetStatusReport().Returns(expectedStatus);

        var presenter = new PenumbraStatusPresenter(mockAnalyzer, mockClient, mockObjectTable);

        // Act
        presenter.RefreshReport();

        // Assert
        Assert.Same(expectedStatus, presenter.CurrentStatus);
    }
}