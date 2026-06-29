namespace Armoire.Tests.Features.ConflictEngine;

using Armoire.Features.ConflictEngine;
using Armoire.Features.LocalScanner;
using Armoire.Features.LocalScanner.Models;
using Armoire.Features.PenumbraIpc;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using NSubstitute;
using System.Collections.Generic;
using Xunit;

public class PenumbraSyncManagerTests {
    private readonly IFramework mockFramework;
    private readonly IPenumbraAnalyzer mockAnalyzer;
    private readonly IPenumbraClient mockPenumbraClient;
    private readonly IPenumbraRepository mockRepository;
    private readonly IObjectTable mockObjectTable;
    private readonly IModScannerManager mockScannerManager;

    public PenumbraSyncManagerTests() {
        // 1. ARRANGE (Global Setup)
        this.mockFramework = Substitute.For<IFramework>();
        this.mockAnalyzer = Substitute.For<IPenumbraAnalyzer>();
        this.mockPenumbraClient = Substitute.For<IPenumbraClient>();
        this.mockRepository = Substitute.For<IPenumbraRepository>();
        this.mockObjectTable = Substitute.For<IObjectTable>();
        this.mockScannerManager = Substitute.For<IModScannerManager>();
    }

    [Fact]
    public void Constructor_WhenPenumbraIsAlreadyEnabled_TriggersBackgroundScan() {
        // Arrange
        this.mockPenumbraClient.IsEnabled().Returns(true);
        this.mockPenumbraClient.GetModsCount().Returns(10);
        this.mockScannerManager.State.Returns(ScanState.Idle);

        // Act
        using var manager = new PenumbraSyncManager(
            this.mockFramework, this.mockAnalyzer, this.mockPenumbraClient,
            this.mockRepository, this.mockObjectTable, this.mockScannerManager);

        // Assert
        this.mockScannerManager.Received(1).InitializeScanProgress(10);
        this.mockScannerManager.Received(1).StartScanAsync();
    }

    [Fact]
    public void ForceRefresh_MarksRepositoryAsStale() {
        // Arrange
        using var manager = new PenumbraSyncManager(
            this.mockFramework, this.mockAnalyzer, this.mockPenumbraClient,
            this.mockRepository, this.mockObjectTable, this.mockScannerManager);

        // Act
        manager.ForceRefresh();

        // Assert
        this.mockRepository.Received(1).MarkStale();
    }

    [Fact]
    public void OnFrameworkUpdate_WhenPlayerChanges_TriggersRepositorySync() {
        // Arrange
        using var manager = new PenumbraSyncManager(
            this.mockFramework, this.mockAnalyzer, this.mockPenumbraClient,
            this.mockRepository, this.mockObjectTable, this.mockScannerManager);

        // Simulate that the repository is NOT currently loading, allowing checks to pass
        this.mockRepository.IsLoading.Returns(false);
        this.mockRepository.IsStale.Returns(false);

        // Mock a valid player login
        var mockPlayer = Substitute.For<IPlayerCharacter>();
        mockPlayer.Name.Returns((Dalamud.Game.Text.SeStringHandling.SeString)"Ysaline Sylv'anir");
        this.mockObjectTable.LocalPlayer.Returns(mockPlayer);

        this.mockPenumbraClient.GetRawModsList().Returns(new Dictionary<string, string>());
        this.mockPenumbraClient.IsEnabled().Returns(true);

        // Act
        // Fire the framework update event (simulating a game tick)
        this.mockFramework.Update += Raise.Event<IFramework.OnUpdateDelegate>(this.mockFramework);

        // Assert
        // Give the async Task.Run inside OnFrameworkUpdate a brief moment to trigger
        System.Threading.Thread.Sleep(50);
        this.mockRepository.Received().SyncDataAsync();
    }
}