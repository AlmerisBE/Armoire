namespace Armoire.Tests.Features.ModDetails;

using Armoire.Core.Localization;
using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.GameData;
using Armoire.Features.LocalScanner;
using Armoire.Features.LocalScanner.Models;
using Armoire.Features.ModDetails;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class ModDetailsResolverTests {
    private readonly IModScannerManager mockScanner;
    private readonly IGameDataService mockGameData;
    private readonly ILocalizationService mockLoc;
    private readonly ArmoireConfiguration realConfig;

    public ModDetailsResolverTests() {
        this.mockScanner = Substitute.For<IModScannerManager>();
        this.mockGameData = Substitute.For<IGameDataService>();
        this.mockLoc = Substitute.For<ILocalizationService>();
        this.realConfig = new ArmoireConfiguration();

        // Simulate basic localization to avoid nulls
        this.mockLoc.GetString(Arg.Any<string>()).Returns(x => x.Arg<string>());

        // Configure the GameDataService to return mock object data
        this.mockGameData.ResolveItem(Arg.Any<string>()).Returns(callInfo => {
            var path = callInfo.Arg<string>();
            return new ResolvedItem {
                Name = "Test Object",
                SlotKey = path.Contains("_top") ? "top" : "unknown",
                IconId = 1234,
                ItemId = 5678
            };
        });
    }

    [Fact]
    public void ResolveModDetails_WhenModNotInScannerCache_ReturnsNull() {
        // Arrange
        this.mockScanner.ModCache.Returns(new Dictionary<string, ArmoireModCacheEntry>());
        var state = new EffectiveCollectionState();
        var resolver = new ModDetailsResolver(this.mockScanner, this.mockGameData, this.mockLoc, this.realConfig);

        // Act
        var result = resolver.ResolveModDetails("UnknownMod", state);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveModDetails_WhenModNotInEffectiveState_ReturnsNull() {
        // Arrange
        var cache = new Dictionary<string, ArmoireModCacheEntry> {
            { "ModA", new ArmoireModCacheEntry { ModName = "ModA" } }
        };
        this.mockScanner.ModCache.Returns(cache);
        var state = new EffectiveCollectionState(); // ModA is not in the effective state
        var resolver = new ModDetailsResolver(this.mockScanner, this.mockGameData, this.mockLoc, this.realConfig);

        // Act
        var result = resolver.ResolveModDetails("ModA", state);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveModDetails_WithHigherPriorityConflict_FlagsSlotAsConflicting() {
        // Arrange
        var modACache = new ArmoireModCacheEntry { ModName = "Victim Mod" };
        modACache.ModifiedGamePaths.Add("chara/equipment/e0123/model/c0101e0123_top.mdl");

        var modBCache = new ArmoireModCacheEntry { ModName = "Winner Mod" };
        modBCache.ModifiedGamePaths.Add("chara/equipment/e0123/model/c0101e0123_top.mdl");

        this.mockScanner.ModCache.Returns(new Dictionary<string, ArmoireModCacheEntry> {
            { "ModA", modACache },
            { "ModB", modBCache }
        });

        var state = new EffectiveCollectionState();
        state.EffectiveMods["ModA"] = new PenumbraMod { Id = "ModA", Name = "Victim Mod", IsEnabled = true, Priority = 5 };
        state.EffectiveMods["ModB"] = new PenumbraMod { Id = "ModB", Name = "Winner Mod", IsEnabled = true, Priority = 10 };

        state.FileOwnership["chara/equipment/e0123/model/c0101e0123_top.mdl"] = new HashSet<string> { "ModA", "ModB" };

        var resolver = new ModDetailsResolver(this.mockScanner, this.mockGameData, this.mockLoc, this.realConfig);

        // Act
        var result = resolver.ResolveModDetails("ModA", state);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.ReplacedSlots);

        var slot = result.ReplacedSlots.First();
        Assert.True(slot.IsConflicting);
        Assert.Contains(slot.OverwrittenByMods, name => name.Contains("Winner Mod"));
    }

    [Fact]
    public void ResolveModDetails_WithMissingTextures_FlagsCorrectlyAndFindsProviders() {
        // Arrange
        // ModA: Provides a 3D model (.mdl) but NO textures (.mtrl/.tex)
        var modACache = new ArmoireModCacheEntry { ModName = "Textureless Mod" };
        modACache.ModifiedGamePaths.Add("chara/equipment/e0500/model/c0101e0500_top.mdl");

        // ModB: Does not provide a model, but provides materials for the same e0500 outfit
        var modBCache = new ArmoireModCacheEntry { ModName = "Texture Pack" };
        modBCache.ModifiedGamePaths.Add("chara/equipment/e0500/material/v0001/mt_c0101e0500_top_a.mtrl");

        this.mockScanner.ModCache.Returns(new Dictionary<string, ArmoireModCacheEntry> {
            { "ModA", modACache },
            { "ModB", modBCache }
        });

        var state = new EffectiveCollectionState();
        state.EffectiveMods["ModA"] = new PenumbraMod { Id = "ModA", Name = "Textureless Mod", IsEnabled = true, Priority = 10 };
        state.EffectiveMods["ModB"] = new PenumbraMod { Id = "ModB", Name = "Texture Pack", IsEnabled = true, Priority = 5 };

        var resolver = new ModDetailsResolver(this.mockScanner, this.mockGameData, this.mockLoc, this.realConfig);

        // Act
        var result = resolver.ResolveModDetails("ModA", state);

        // Assert
        Assert.NotNull(result);
        var slot = result.ReplacedSlots.First();

        // 1. It must detect that textures are missing for e0500
        Assert.True(slot.IsMissingTextures);

        // 2. It must have scanned ModB and found that it contains a .mtrl for e0500
        Assert.True(slot.AvailableTextureProviders.ContainsKey("ModB"));
        Assert.Equal("Texture Pack", slot.AvailableTextureProviders["ModB"]);
    }
}