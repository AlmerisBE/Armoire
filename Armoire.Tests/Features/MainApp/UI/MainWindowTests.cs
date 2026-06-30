namespace Armoire.Core.Tests.UI;

using Armoire.Core.Localization;
using Armoire.Features.DiagnosticsUI.Presentation;
using Armoire.Features.MainApp.Presentation;
using Armoire.Features.MainApp.UI;
using NSubstitute;
using Xunit;

public class MainWindowTests {
    [Fact]
    public void InvokeConfigRequested_ShouldTriggerOnConfigRequestedEvent() {
        // Arrange
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockPresenter = Substitute.For<IMainWindowPresenter>();
        var mockStatusPresenter = Substitute.For<IPenumbraStatusPresenter>();

        var mainWindow = new MainWindow(mockLocalization, mockPresenter, mockStatusPresenter);
        bool eventTriggered = false;
        mainWindow.OnConfigRequested += () => eventTriggered = true;

        // Act
        mainWindow.InvokeConfigRequested();

        // Assert
        Assert.True(eventTriggered);
    }
}