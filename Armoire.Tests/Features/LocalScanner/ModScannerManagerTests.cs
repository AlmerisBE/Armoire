namespace Armoire.Tests.Features.LocalScanner;

using Armoire.Features.LocalScanner;
using Armoire.Features.LocalScanner.Models;
using Armoire.Features.PenumbraIpc;
using Dalamud.Plugin.Services;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

public class ModScannerManagerTests : IDisposable {
    private readonly string tempModDirectory;
    private readonly IPenumbraClient mockPenumbraClient;
    private readonly IPluginLog mockPluginLog;
    private readonly INotificationManager mockNotificationManager;

    public ModScannerManagerTests() {
        // 1. ARRANGE (Global Setup)
        this.tempModDirectory = Path.Combine(Path.GetTempPath(), "ArmoireTests_Scanner_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(this.tempModDirectory);

        this.mockPenumbraClient = Substitute.For<IPenumbraClient>();
        this.mockPluginLog = Substitute.For<IPluginLog>();
        this.mockNotificationManager = Substitute.For<INotificationManager>();

        // Mock the IPC client to return our isolated temp directory
        this.mockPenumbraClient.GetModDirectory().Returns(this.tempModDirectory);
    }

    public void Dispose() {
        // CLEANUP: Ensure no junk is left on the developer's / user's drive
        if (Directory.Exists(this.tempModDirectory)) {
            Directory.Delete(this.tempModDirectory, true);
        }
    }

    [Fact]
    public void InitializeScanProgress_SetsCorrectTotalMods() {
        // Arrange
        var mockModList = new Dictionary<string, string> {
            { "ModA", "Awesome Mod A" },
            { "ModB", "Brilliant Mod B" }
        };
        this.mockPenumbraClient.GetRawModsList().Returns(mockModList);

        using var scanner = new ModScannerManager(this.mockPenumbraClient, this.mockPluginLog, this.mockNotificationManager);

        // Act
        scanner.InitializeScanProgress(5); // The 5 should be overridden by the actual IPC list count (2)

        // Assert
        Assert.Equal(2, scanner.TotalMods);
        Assert.Equal(0, scanner.ProcessedMods);
        Assert.Equal(ScanState.Idle, scanner.State);
    }

    [Fact]
    public async Task StartScanAsync_WithValidMod_PopulatesCacheCorrectly() {
        // Arrange
        var modDir = Path.Combine(this.tempModDirectory, "ValidModFolder");
        Directory.CreateDirectory(modDir);

        // Create a fake default_mod.json
        var defaultJson = "{\"Files\": {\"chara/equipment/e001/model/c0101e001_top.mdl\": \"custom_top.mdl\"}}";
        File.WriteAllText(Path.Combine(modDir, "default_mod.json"), defaultJson);

        // Create a fake group option
        var groupJson = "{\"Name\": \"Colors\", \"Type\": \"Single\", \"Options\": [{\"Name\": \"Red\", \"Files\": {\"chara/weapon/w001.mdl\": \"red_w001.mdl\"}}]}";
        File.WriteAllText(Path.Combine(modDir, "group_001.json"), groupJson);

        var mockModList = new Dictionary<string, string> { { "ValidModFolder", "My Valid Mod" } };
        this.mockPenumbraClient.GetRawModsList().Returns(mockModList);

        using var scanner = new ModScannerManager(this.mockPenumbraClient, this.mockPluginLog, this.mockNotificationManager);
        scanner.InitializeScanProgress(1);

        // Act
        await scanner.StartScanAsync();

        // Assert
        Assert.Equal(1, scanner.ProcessedMods);
        Assert.Equal(0, scanner.ErrorCount);
        Assert.True(scanner.ModCache.ContainsKey("ValidModFolder"));

        var cachedMod = scanner.ModCache["ValidModFolder"];
        Assert.Contains("chara/equipment/e001/model/c0101e001_top.mdl", cachedMod.ModifiedGamePaths);

        Assert.True(cachedMod.OptionGroups.ContainsKey("Colors"));
        Assert.Contains("chara/weapon/w001.mdl", cachedMod.OptionGroups["Colors"].OptionPaths[0]);
    }

    [Fact]
    public async Task StartScanAsync_WithCorruptJson_FailsGracefullyWithoutCrashing() {
        // Arrange
        var modDir = Path.Combine(this.tempModDirectory, "CorruptModFolder");
        Directory.CreateDirectory(modDir);

        // Write intentionally broken JSON
        File.WriteAllText(Path.Combine(modDir, "default_mod.json"), "{ broken json formatting ]");
        File.WriteAllText(Path.Combine(modDir, "group_001.json"), "Not even JSON");

        var mockModList = new Dictionary<string, string> { { "CorruptModFolder", "Corrupt Mod" } };
        this.mockPenumbraClient.GetRawModsList().Returns(mockModList);

        using var scanner = new ModScannerManager(this.mockPenumbraClient, this.mockPluginLog, this.mockNotificationManager);
        scanner.InitializeScanProgress(1);

        // Act
        await scanner.StartScanAsync();

        // Assert
        Assert.Equal(1, scanner.ProcessedMods);

        // The engine rightfully rejects the corrupt mod and flags it as an error.
        Assert.Equal(1, scanner.ErrorCount);
        Assert.False(scanner.ModCache.ContainsKey("CorruptModFolder"));
    }
}