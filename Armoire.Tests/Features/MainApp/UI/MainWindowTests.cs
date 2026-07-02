namespace Armoire.Core.Tests.UI;

using Armoire.Core.Localization;
using Armoire.Features.DiagnosticsUI.Presentation;
using Armoire.Features.MainApp.Presentation;
using Armoire.Features.MainApp.UI;
using Armoire.Features.MainApp.UI.Tabs;
using Armoire.Features.Outfits.Presentation;
using NSubstitute;
using Xunit;

public class MainWindowTests {
    [Fact]
    public void InvokeConfigRequested_ShouldTriggerOnConfigRequestedEvent() {
        // Arrange
        var mockLocalization = Substitute.For<ILocalizationService>();
        var mockMainPresenter = Substitute.For<IMainWindowPresenter>();
        var mockStatusPresenter = Substitute.For<IPenumbraStatusPresenter>();
        var mockOutfitsPresenter = Substitute.For<IOutfitsPresenter>();
        var mockOutfitDetailsPresenter = Substitute.For<IOutfitDetailsPresenter>();
        var mockOutfitImportPresenter = Substitute.For<IOutfitImportPresenter>();

        // Instantiate the tab components using our mocked dependencies
        var homeTab = new HomeTab(mockLocalization, mockMainPresenter);
        var resolvedTab = new ResolvedTab(mockLocalization, mockMainPresenter);
        var statsTab = new StatsTab(mockLocalization, mockMainPresenter, mockStatusPresenter, mockOutfitsPresenter);
        var configTab = new ConfigTab(mockLocalization, mockMainPresenter);
        var aboutTab = new AboutTab(mockLocalization, mockMainPresenter);

        // Inject the new mock into the OutfitsTab constructor
        var outfitsTab = new OutfitsTab(mockLocalization, mockOutfitsPresenter, mockOutfitDetailsPresenter, mockOutfitImportPresenter);

        // Inject all the required tabs into the MainWindow constructor
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

        bool eventTriggered = false;
        mainWindow.OnConfigRequested += () => eventTriggered = true;

        // Act
        mainWindow.InvokeConfigRequested();

        // Assert
        Assert.True(eventTriggered);
    }
}