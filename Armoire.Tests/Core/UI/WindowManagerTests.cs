namespace Armoire.Core.Tests.UI;

using Armoire.Core.Localization;
using Armoire.Core.UI;
using Armoire.Features.DiagnosticsUI.Presentation;
using Armoire.Features.MainApp.Presentation;
using Armoire.Features.MainApp.UI;
using NSubstitute;
using Xunit;

public class WindowManagerTests {
    [Fact]
    public void OnConfigRequested_ShouldToggleConfigWindowVisibility() {
        // Arrange
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockMainPresenter = Substitute.For<IMainWindowPresenter>();
        var mockStatusPresenter = Substitute.For<IPenumbraStatusPresenter>();

        // Inject the newly required presenter mocks into the MainWindow constructor
        var mainWindow = new MainWindow(mockLocalization, mockMainPresenter, mockStatusPresenter);
        var configWindow = new ConfigWindow();

        var windowManager = new WindowManager(mainWindow, configWindow);

        var initialState = configWindow.IsOpen;

        // Act
        mainWindow.InvokeConfigRequested();

        // Assert
        Assert.NotEqual(initialState, configWindow.IsOpen);
    }
}