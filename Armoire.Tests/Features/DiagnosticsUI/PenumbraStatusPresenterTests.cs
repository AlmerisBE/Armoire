namespace Armoire.Tests.Features.DiagnosticsUI;

using Armoire.Features.ConflictEngine;
using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.DiagnosticsUI.Presentation;
using Armoire.Features.LocalScanner;
using NSubstitute;
using System;
using Xunit;

public class PenumbraStatusPresenterTests {
    [Fact]
    public void Constructor_EventSubscription_UpdatesPresenterState() {
        // Arrange
        var mockSyncManager = Substitute.For<IPenumbraSyncManager>();
        var mockScannerManager = Substitute.For<IModScannerManager>();
        var mockConflictPresenter = Substitute.For<IConflictListPresenter>();

        var presenter = new PenumbraStatusPresenter(mockSyncManager, mockScannerManager, mockConflictPresenter);

        var expectedStatus = new PenumbraStatusResult {
            IsEnabled = true,
            ModCount = 42,
            PlayerName = "Almeris Test"
        };

        // Act
        // Simulate the background manager triggering the event
        mockSyncManager.OnStatusUpdated += Raise.Event<Action<PenumbraStatusResult>>(expectedStatus);

        // Assert
        Assert.Equal("Active", presenter.IntegrationStatus);
        Assert.Equal(42, presenter.TotalIpcMods);
        Assert.Equal("Almeris Test", presenter.ConnectedCharacter);
    }

    [Fact]
    public void RefreshReport_CallsForceRefreshOnManager() {
        // Arrange
        var mockSyncManager = Substitute.For<IPenumbraSyncManager>();
        var mockScannerManager = Substitute.For<IModScannerManager>();
        var mockConflictPresenter = Substitute.For<IConflictListPresenter>();

        var presenter = new PenumbraStatusPresenter(mockSyncManager, mockScannerManager, mockConflictPresenter);

        // Act
        presenter.RefreshReport();

        // Assert
        mockSyncManager.Received(1).ForceRefresh();
    }
}