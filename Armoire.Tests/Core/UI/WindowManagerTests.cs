namespace Armoire.Core.Tests.UI;

using Armoire.Core.Localization;
using Armoire.Core.UI;
using Armoire.Features.MainApp.UI;
using NSubstitute;
using Xunit;

public class WindowManagerTests {
    [Fact]
    public void OnConfigRequested_ShouldToggleConfigWindowVisibility() {
        // Arrange
        var mockLocalization = Substitute.For<ILocalizationService>();

        var mainWindow = new MainWindow(mockLocalization);
        var configWindow = new ConfigWindow();

        var windowManager = new WindowManager(mainWindow, configWindow);

        var initialState = configWindow.IsOpen;

        // Act
        mainWindow.InvokeConfigRequested();

        // Assert
        Assert.NotEqual(initialState, configWindow.IsOpen);
    }
}