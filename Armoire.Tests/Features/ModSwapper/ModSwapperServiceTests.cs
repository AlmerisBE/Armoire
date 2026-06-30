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
    private readonly string tempRootDirectory;
    private readonly string modFolderName;
    private readonly string tempModDirectory;
    private readonly IPluginLog mockLog;
    private readonly IModScannerManager mockScanner;
    private readonly IPenumbraClient mockPenumbra;
    private readonly ArmoireConfiguration mockConfiguration;

    public ModSwapperServiceTests() {
        this.tempRootDirectory = Path.GetTempPath();
        this.modFolderName = "ArmoireTests_Swapper_" + Guid.NewGuid().ToString();
        this.tempModDirectory = Path.Combine(this.tempRootDirectory, this.modFolderName);

        Directory.CreateDirectory(this.tempModDirectory);

        this.mockLog = Substitute.For<IPluginLog>();
        this.mockScanner = Substitute.For<IModScannerManager>();
        this.mockPenumbra = Substitute.For<IPenumbraClient>();
        this.mockConfiguration = Substitute.For<ArmoireConfiguration>();

        this.mockPenumbra.GetModDirectory().Returns(this.tempRootDirectory);

        var fakeCache = new Dictionary<string, ArmoireModCacheEntry> {
            { this.modFolderName, new ArmoireModCacheEntry() }
        };
        this.mockScanner.ModCache.Returns(fakeCache);
    }

    public void Dispose() {
        if (Directory.Exists(this.tempModDirectory)) {
            Directory.Delete(this.tempModDirectory, true);
        }
    }

    [Fact]
    public void PerformSwap_WithOptionGroups_ReplacesPathsInAllJsonFiles() {
        // Arrange
        var swapper = new ModSwapperService(this.mockScanner, this.mockLog, this.mockPenumbra, this.mockConfiguration);

        // 1. Create default_mod.json
        string defaultPath = Path.Combine(this.tempModDirectory, "default_mod.json");
        string defaultJson = @"{ ""Files"": { ""chara/equipment/e0123/model/c0101e0123_top.mdl"": ""custom.mdl"" } }";
        File.WriteAllText(defaultPath, defaultJson);

        // 2. Create group_001.json containing options
        string groupPath = Path.Combine(this.tempModDirectory, "group_001.json");
        string groupJson = @"{
            ""Name"": ""Colors"",
            ""Options"": [
                {
                    ""Name"": ""Red"",
                    ""Files"": { ""chara/equipment/e0123/material/v0001/mt_c0101e0123_top_a.mtrl"": ""red.mtrl"" }
                }
            ]
        }";
        File.WriteAllText(groupPath, groupJson);

        // Act
        bool result = swapper.PerformSwap(this.modFolderName, "top", "e0500");

        // Assert
        Assert.True(result);

        // Check default_mod.json
        Assert.True(File.Exists(defaultPath + ".armoire_bak"));
        string newDefault = File.ReadAllText(defaultPath);
        Assert.Contains("e0500_top.mdl", newDefault);

        // Check group_001.json
        Assert.True(File.Exists(groupPath + ".armoire_bak"));
        string newGroup = File.ReadAllText(groupPath);
        Assert.Contains("e0500_top", newGroup);
        Assert.DoesNotContain("e0123_top", newGroup);

        this.mockPenumbra.Received(1).ReloadMod(this.modFolderName);
    }

    [Fact]
    public void ResetMod_WithMultipleBackups_RestoresAllOriginalJsonFiles() {
        // Arrange
        var swapper = new ModSwapperService(this.mockScanner, this.mockLog, this.mockPenumbra, this.mockConfiguration);

        string defaultPath = Path.Combine(this.tempModDirectory, "default_mod.json");
        string groupPath = Path.Combine(this.tempModDirectory, "group_001.json");

        File.WriteAllText(defaultPath + ".armoire_bak", "ORIGINAL_DEFAULT");
        File.WriteAllText(defaultPath, "MODIFIED_DEFAULT");

        File.WriteAllText(groupPath + ".armoire_bak", "ORIGINAL_GROUP");
        File.WriteAllText(groupPath, "MODIFIED_GROUP");

        // Act
        bool result = swapper.ResetMod(this.modFolderName);

        // Assert
        Assert.True(result);

        Assert.Equal("ORIGINAL_DEFAULT", File.ReadAllText(defaultPath));
        Assert.False(File.Exists(defaultPath + ".armoire_bak"));

        Assert.Equal("ORIGINAL_GROUP", File.ReadAllText(groupPath));
        Assert.False(File.Exists(groupPath + ".armoire_bak"));

        this.mockPenumbra.Received(1).ReloadMod(this.modFolderName);
    }
}