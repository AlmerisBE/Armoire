using Xunit;
using NSubstitute;
using Armoire.Core.UI;
using Armoire.Features.MainApp.UI;

namespace Armoire.Core.Tests.UI
{
    public class MainWindowTests
    {
        [Fact]
        public void AttachComponent_ShouldAddComponentToAttachedComponentsList()
        {
            // Arrange
            var mainWindow = new MainWindow();
            var mockComponent = Substitute.For<IUiComponent>();

            // Act
            mainWindow.AttachComponent(mockComponent);

            // Assert
            Assert.Contains(mockComponent, mainWindow.AttachedComponents);
        }
    }
}
