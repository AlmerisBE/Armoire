namespace Armoire.Core.Tests.UI;

using Armoire.Core.Localization;
using Armoire.Core.UI;
using Armoire.Features.DiagnosticsUI.Presentation;
using Armoire.Features.MainApp.Presentation;
using Armoire.Features.MainApp.UI;
using Armoire.Features.MainApp.UI.Tabs;
using Armoire.Features.Outfits.Presentation;
using NSubstitute;
using Xunit;

public class WindowManagerTests {
    [Fact]
    public void OnConfigRequested_ShouldToggleConfigWindowVisibility() {
        // Arrange
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockMainPresenter = Substitute.For<IMainWindowPresenter>();
        var mockStatusPresenter = Substitute.For<IPenumbraStatusPresenter>();
        var mockOutfitsPresenter = Substitute.For<IOutfitsPresenter>();
        var mockOutfitDetailsPresenter = Substitute.For<IOutfitDetailsPresenter>();
        var mockOutfitImportPresenter = Substitute.For<IOutfitImportPresenter>();

        var homeTab = new HomeTab(mockLocalization, mockMainPresenter);
        var resolvedTab = new ResolvedTab(mockLocalization, mockMainPresenter);
        var statsTab = new StatsTab(mockLocalization, mockMainPresenter, mockStatusPresenter, mockOutfitsPresenter);
        var configTab = new ConfigTab(mockLocalization, mockMainPresenter);
        var aboutTab = new AboutTab(mockLocalization, mockMainPresenter);

        var outfitsTab = new OutfitsTab(mockLocalization, mockOutfitsPresenter, mockOutfitDetailsPresenter, mockOutfitImportPresenter);

        var mainWindow = new MainWindow(
            mockLocalization,
            mockMainPresenter,
            homeTab,
            resolvedTab,
            statsTab,
            configTab,
            aboutTab,
            outfitsTab
        );

        var configWindow = new ConfigWindow();

        // Inject null! for the standalone windows since they are completely irrelevant to this test
        var windowManager = new WindowManager(
            mainWindow,
            configWindow,
            null!, // modDetailsWindow
            null!, // vanillaReplacementWindow
            null!, // modScannerWindow
            null!, // outfitDetailsWindow
            null!  // importWindow
        );

        var initialState = configWindow.IsOpen;

        // Act
        mainWindow.InvokeConfigRequested();

        // Assert
        Assert.NotEqual(initialState, configWindow.IsOpen);
    }
}