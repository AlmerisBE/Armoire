namespace Armoire.Core.Tests.UI;

using Armoire.Core.Localization;
using Armoire.Core.UI;
using Armoire.Features.DiagnosticsUI.Presentation;
using Armoire.Features.MainApp.Presentation;
using Armoire.Features.MainApp.UI;
using Armoire.Features.MainApp.UI.Tabs;
using NSubstitute;
using Xunit;

public class WindowManagerTests {
    [Fact]
    public void OnConfigRequested_ShouldToggleConfigWindowVisibility() {
        // Arrange
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockMainPresenter = Substitute.For<IMainWindowPresenter>();
        var mockStatusPresenter = Substitute.For<IPenumbraStatusPresenter>();

        var homeTab = new HomeTab(mockLocalization, mockMainPresenter);
        var resolvedTab = new ResolvedTab(mockLocalization, mockMainPresenter);
        var statsTab = new StatsTab(mockLocalization, mockMainPresenter, mockStatusPresenter);
        var configTab = new ConfigTab(mockLocalization, mockMainPresenter);
        var aboutTab = new AboutTab(mockLocalization, mockMainPresenter);

        var mainWindow = new MainWindow(
            mockLocalization,
            mockMainPresenter,
            homeTab,
            resolvedTab,
            statsTab,
            configTab,
            aboutTab
        );

        var configWindow = new ConfigWindow();

        // Inject null! for the standalone windows since they are completely irrelevant to this test
        var windowManager = new WindowManager(
            mainWindow,
            configWindow,
            null!, // modDetailsWindow
            null!, // vanillaReplacementWindow
            null!  // modScannerWindow
        );

        var initialState = configWindow.IsOpen;

        // Act
        mainWindow.InvokeConfigRequested();

        // Assert
        Assert.NotEqual(initialState, configWindow.IsOpen);
    }
}