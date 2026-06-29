namespace Armoire.Tests.Features.PenumbraIpc;

using Armoire.Features.PenumbraIpc;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using NSubstitute;
using System;
using System.Collections.Generic;
using Xunit;

public class PenumbraClientTests {
    private readonly IDalamudPluginInterface mockPluginInterface;
    private readonly IPluginLog mockPluginLog;

    // We keep references to the mock subscribers to manipulate their behavior during tests
    private readonly ICallGateSubscriber<Action> mockActionSubscriber;
    private readonly ICallGateSubscriber<(int, int)> mockApiVersionSubscriber;
    private readonly ICallGateSubscriber<string> mockModDirectorySubscriber;
    private readonly ICallGateSubscriber<IReadOnlyDictionary<string, string>> mockModListSubscriber;

    public PenumbraClientTests() {
        this.mockPluginInterface = Substitute.For<IDalamudPluginInterface>();
        this.mockPluginLog = Substitute.For<IPluginLog>();

        // 1. ARRANGE (Global Setup)
        // We must mock the generic GetIpcSubscriber methods so Penumbra's API wrappers don't throw NullReferenceExceptions during construction.

        this.mockActionSubscriber = Substitute.For<ICallGateSubscriber<Action>>();
        this.mockPluginInterface.GetIpcSubscriber<Action>(Arg.Any<string>()).Returns(this.mockActionSubscriber);

        this.mockApiVersionSubscriber = Substitute.For<ICallGateSubscriber<(int, int)>>();
        this.mockPluginInterface.GetIpcSubscriber<(int, int)>(Arg.Any<string>()).Returns(this.mockApiVersionSubscriber);

        this.mockModDirectorySubscriber = Substitute.For<ICallGateSubscriber<string>>();
        this.mockPluginInterface.GetIpcSubscriber<string>(Arg.Any<string>()).Returns(this.mockModDirectorySubscriber);

        this.mockModListSubscriber = Substitute.For<ICallGateSubscriber<IReadOnlyDictionary<string, string>>>();
        this.mockPluginInterface.GetIpcSubscriber<IReadOnlyDictionary<string, string>>(Arg.Any<string>()).Returns(this.mockModListSubscriber);

        // Note: GetCollectionForObject uses complex generic tuples internally in the Penumbra API wrapper. 
        // We skip strict mocking for it here to focus on core failure handling.
    }

    [Fact]
    public void Constructor_SubscribesToLifecycleEvents() {
        // Act
        using var client = new PenumbraClient(this.mockPluginInterface, this.mockPluginLog);

        // Assert
        // We verify that our wrapper correctly registered its listeners to the IPC Action channels
        this.mockActionSubscriber.Received().Subscribe(Arg.Any<Action>());
    }

    [Fact]
    public void IsEnabled_WhenIpcThrowsException_ReturnsFalse() {
        // Arrange
        // Simulate Penumbra being unloaded or an IPC channel crash
        this.mockApiVersionSubscriber.When(x => x.InvokeFunc()).Throw(new Exception("IPC Channel Not Found"));
        using var client = new PenumbraClient(this.mockPluginInterface, this.mockPluginLog);

        // Act
        bool result = client.IsEnabled();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsEnabled_WhenIpcSucceeds_ReturnsTrue() {
        // Arrange
        this.mockApiVersionSubscriber.InvokeFunc().Returns((1, 0));
        using var client = new PenumbraClient(this.mockPluginInterface, this.mockPluginLog);

        // Act
        bool result = client.IsEnabled();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetModDirectory_WhenIpcThrowsException_ReturnsEmptyStringAndLogsWarning() {
        // Arrange
        // First, ensure IsEnabled() passes so it attempts the call
        this.mockApiVersionSubscriber.InvokeFunc().Returns((1, 0));

        // Then, simulate a crash specifically on the directory fetching
        this.mockModDirectorySubscriber.When(x => x.InvokeFunc()).Throw(new Exception("Permission Denied"));

        using var client = new PenumbraClient(this.mockPluginInterface, this.mockPluginLog);

        // Act
        string result = client.GetModDirectory();

        // Assert
        Assert.Equal(string.Empty, result);
        this.mockPluginLog.Received(1).Warning(Arg.Any<Exception>(), Arg.Any<string>());
    }

    [Fact]
    public void GetRawModsList_WhenIpcThrowsException_ReturnsEmptyDictionaryAndLogsError() {
        // Arrange
        this.mockApiVersionSubscriber.InvokeFunc().Returns((1, 0));

        this.mockModListSubscriber.InvokeFunc().Returns(x => throw new Exception("Timeout"));

        using var client = new PenumbraClient(this.mockPluginInterface, this.mockPluginLog);

        // Act
        var result = client.GetRawModsList();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        // On vérifie que notre NOUVEAU message de secours a bien été envoyé dans les logs
        this.mockPluginLog.ReceivedWithAnyArgs().Error(null!);
    }

    [Fact]
    public void Dispose_UnsubscribesFromLifecycleEvents() {
        // Arrange
        var client = new PenumbraClient(this.mockPluginInterface, this.mockPluginLog);

        // Act
        client.Dispose();

        // Assert
        this.mockActionSubscriber.Received().Unsubscribe(Arg.Any<Action>());
    }
}