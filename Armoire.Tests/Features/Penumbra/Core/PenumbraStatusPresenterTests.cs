using Armoire.Features.Penumbra.Interfaces;
using Armoire.Features.Penumbra.UI;
using Dalamud.Plugin.Services;
using NSubstitute;
using Xunit;

namespace Armoire.Features.Penumbra.Tests;

public class PenumbraStatusPresenterTests {
    [Fact]
    public void RefreshReport_ShouldUpdateCurrentReportFromAnalyzer() {
        // Arrange
        var mockAnalyzer = Substitute.For<IPenumbraAnalyzer>();
        var mockClient = Substitute.For<IPenumbraClient>();
        var mockObjectTable = Substitute.For<IObjectTable>();

        var expectedReport = "Penumbra is running. Mods loaded: 42";
        mockAnalyzer.GetStatusReport().Returns(expectedReport);

        var presenter = new PenumbraStatusPresenter(mockAnalyzer, mockClient, mockObjectTable);

        // Act
        presenter.RefreshReport();

        // Assert
        Assert.Equal(expectedReport, presenter.CurrentReport);
    }
}
