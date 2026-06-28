using Armoire.Core.UI;
using Armoire.Features.MainApp.UI;
using NSubstitute;
using Xunit;

namespace Armoire.Core.Tests.UI;

public class MainWindowTests {
    [Fact]
    public void AttachComponent_ShouldAddComponentToAttachedComponentsList() {
        // Arrange
        var mainWindow = new MainWindow();
        var mockComponent = Substitute.For<IUiComponent>();

        // Act
        mainWindow.AttachComponent(mockComponent);

        // Assert
        Assert.Contains(mockComponent, mainWindow.AttachedComponents);
    }
}
