namespace Armoire.Core.Tests.UI;

using Armoire.Core.Localization;
using Armoire.Core.UI;
using Armoire.Features.MainApp.UI;
using NSubstitute;
using Xunit;

public class MainWindowTests {
    [Fact]
    public void AttachComponent_ShouldAddComponentToAttachedComponentsList() {
        // Arrange
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mainWindow = new MainWindow(mockLocalization);

        var mockComponent = Substitute.For<IUiComponent>();

        // Act
        mainWindow.AttachComponent(mockComponent);

        // Assert
        Assert.Contains(mockComponent, mainWindow.AttachedComponents);
    }
}