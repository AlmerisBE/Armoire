namespace Armoire.Tests.Features.ModSwapper;

using Armoire.Features.LocalScanner;
using Armoire.Features.LocalScanner.Models;
using Armoire.Features.ModSwapper;
using Armoire.Features.PenumbraIpc;
using Dalamud.Plugin.Services;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

public class ModSwapperServiceTests : IDisposable {
    private readonly string tempModDirectory;
    private readonly IPluginLog mockLog;
    private readonly IModScannerManager mockScanner;
    private readonly IPenumbraClient mockPenumbra;

    public ModSwapperServiceTests() {
        this.tempModDirectory = Path.Combine(Path.GetTempPath(), "ArmoireTests_Swapper_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(this.tempModDirectory);

        this.mockLog = Substitute.For<IPluginLog>();
        this.mockScanner = Substitute.For<IModScannerManager>();
        this.mockPenumbra = Substitute.For<IPenumbraClient>();

        var fakeCache = new Dictionary<string, ArmoireModCacheEntry> {
            { this.tempModDirectory, new ArmoireModCacheEntry() }
        };
        this.mockScanner.ModCache.Returns(fakeCache);
    }

    public void Dispose() {
        if (Directory.Exists(this.tempModDirectory)) {
            Directory.Delete(this.tempModDirectory, true);
        }
    }

    [Fact]
    public void PerformSwap_WithValidJson_ReplacesPathsAndCreatesBackup() {
        // Arrange
        var swapper = new ModSwapperService(this.mockScanner, this.mockLog, this.mockPenumbra);
        string configPath = Path.Combine(this.tempModDirectory, "default_mod.json");
        string backupPath = Path.Combine(this.tempModDirectory, "default_mod.armoire_bak");

        string initialJson = @"{
            ""Files"": {
                ""chara/equipment/e0123/model/c0101e0123_top.mdl"": ""custom.mdl"",
                ""chara/equipment/e0123/material/v0001/mt_c0101e0123_top_a.mtrl"": ""custom.mtrl""
            }
        }";
        File.WriteAllText(configPath, initialJson);

        // Act
        bool result = swapper.PerformSwap(this.tempModDirectory, "top", "e0500");

        // Assert
        Assert.True(result);
        Assert.True(File.Exists(backupPath)); // Backup created

        string newJson = File.ReadAllText(configPath);
        Assert.Contains("e0500_top.mdl", newJson); // Replaced successfully
        Assert.DoesNotContain("e0123_top.mdl", newJson);

        // Verify IPC Calls
        this.mockPenumbra.Received(1).ReloadMod(this.tempModDirectory);
        this.mockPenumbra.Received(1).RedrawAll();
    }

    [Fact]
    public void ResetMod_WithExistingBackup_RestoresOriginalJson() {
        // Arrange
        var swapper = new ModSwapperService(this.mockScanner, this.mockLog, this.mockPenumbra);
        string configPath = Path.Combine(this.tempModDirectory, "default_mod.json");
        string backupPath = Path.Combine(this.tempModDirectory, "default_mod.armoire_bak");

        File.WriteAllText(backupPath, "ORIGINAL_DATA");
        File.WriteAllText(configPath, "MODIFIED_DATA");

        // Act
        bool result = swapper.ResetMod(this.tempModDirectory);

        // Assert
        Assert.True(result);
        Assert.Equal("ORIGINAL_DATA", File.ReadAllText(configPath));
        Assert.False(File.Exists(backupPath)); // Backup consumed/deleted

        this.mockPenumbra.Received(1).ReloadMod(this.tempModDirectory);
        this.mockPenumbra.Received(1).RedrawAll();
    }
}