using Armoire.Core.UI;
using Armoire.Features.MainApp.UI;
using Xunit;

namespace Armoire.Core.Tests.UI;

public class WindowManagerTests {
    [Fact]
    public void OnConfigRequested_ShouldToggleConfigWindowVisibility() {
        // Arrange
        var mainWindow = new MainWindow();
        var configWindow = new ConfigWindow();

        var windowManager = new WindowManager(mainWindow, configWindow);

        var initialState = configWindow.IsOpen;

        // Act
        mainWindow.InvokeConfigRequested();

        // Assert
        Assert.NotEqual(initialState, configWindow.IsOpen);
    }
}
