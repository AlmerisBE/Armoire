namespace Armoire.Core.Tests.UI;

using Armoire.Core.Localization;
using Armoire.Features.DiagnosticsUI.Presentation;
using Armoire.Features.MainApp.Presentation;
using Armoire.Features.MainApp.UI;
using Armoire.Features.MainApp.UI.Tabs;
using NSubstitute;
using Xunit;

public class MainWindowTests {
    [Fact]
    public void InvokeConfigRequested_ShouldTriggerOnConfigRequestedEvent() {
        // Arrange
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockMainPresenter = Substitute.For<IMainWindowPresenter>();
        var mockStatusPresenter = Substitute.For<IPenumbraStatusPresenter>();

        // Instantiate the tab components using our mocked dependencies
        var homeTab = new HomeTab(mockLocalization, mockMainPresenter);
        var resolvedTab = new ResolvedTab(mockLocalization, mockMainPresenter);
        var statsTab = new StatsTab(mockLocalization, mockMainPresenter, mockStatusPresenter);
        var configTab = new ConfigTab(mockLocalization, mockMainPresenter);
        var aboutTab = new AboutTab(mockLocalization, mockMainPresenter);

        // Inject all the newly required tabs into the MainWindow constructor
        var mainWindow = new MainWindow(
            mockLocalization,
            mockMainPresenter,
            homeTab,
            resolvedTab,
            statsTab,
            configTab,
            aboutTab
        );

        bool eventTriggered = false;
        mainWindow.OnConfigRequested += () => eventTriggered = true;

        // Act
        mainWindow.InvokeConfigRequested();

        // Assert
        Assert.True(eventTriggered);
    }
}