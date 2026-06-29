namespace Armoire.Tests.Features.DiagnosticsUI;

using Armoire.Features.ConflictEngine;
using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.DiagnosticsUI;
using NSubstitute;
using System;
using Xunit;

public class PenumbraStatusPresenterTests {
    [Fact]
    public void Constructeur_AbonnementEvent_MetAJourCurrentStatus() {
        // Arrange
        var mockSyncManager = Substitute.For<IPenumbraSyncManager>();
        var presenter = new PenumbraStatusPresenter(mockSyncManager);

        var statutAttendu = new PenumbraStatusResult {
            IsEnabled = true,
            ModCount = 42,
            PlayerName = "Almeris Test"
        };

        // Act
        // On simule le déclenchement de l'événement par le Manager d'arrière-plan
        mockSyncManager.OnStatusUpdated += Raise.Event<Action<PenumbraStatusResult>>(statutAttendu);

        // Assert
        Assert.Same(statutAttendu, presenter.CurrentStatus);
    }

    [Fact]
    public void RefreshReport_AppelleForceRefreshSurLeManager() {
        // Arrange
        var mockSyncManager = Substitute.For<IPenumbraSyncManager>();
        var presenter = new PenumbraStatusPresenter(mockSyncManager);

        // Act
        presenter.RefreshReport();

        // Assert
        mockSyncManager.Received(1).ForceRefresh();
    }
}