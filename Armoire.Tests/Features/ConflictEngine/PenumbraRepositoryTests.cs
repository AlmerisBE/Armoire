namespace Armoire.Tests.Features.ConflictEngine;

using Armoire.Features.ConflictEngine;
using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.LocalScanner;
using Armoire.Features.LocalScanner.Models;
using Dalamud.Plugin.Services;
using NSubstitute;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

public class PenumbraRepositoryTests {
    private readonly IPluginLog mockPluginLog;
    private readonly IModScannerManager mockScannerManager;

    public PenumbraRepositoryTests() {
        this.mockPluginLog = Substitute.For<IPluginLog>();
        this.mockScannerManager = Substitute.For<IModScannerManager>();
    }

    // Helper method to inject fake data into the repository's private cache to avoid disk I/O during tests
    private void InjectMockCache(PenumbraRepository repository, Dictionary<string, PenumbraCollection> fakeCache) {
        var field = typeof(PenumbraRepository).GetField("collectionCache", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(repository, fakeCache);
    }

    [Fact]
    public void ComputeEffectiveState_WithBasicConflict_IdentifiesLoserAndSlot() {
        // Arrange
        var repo = new PenumbraRepository(this.mockPluginLog, this.mockScannerManager);

        // 1. Setup the fake Scanner data (The "Hardware" reality)
        var fakeScannerCache = new Dictionary<string, ArmoireModCacheEntry> {
            { "ModA", new ArmoireModCacheEntry { ModName = "Winner Mod", ModifiedGamePaths = new HashSet<string> { "chara/weapon/w001.mdl" } } },
            { "ModB", new ArmoireModCacheEntry { ModName = "Loser Mod", ModifiedGamePaths = new HashSet<string> { "chara/weapon/w001.mdl" } } }
        };
        this.mockScannerManager.ModCache.Returns(fakeScannerCache);

        // 2. Setup the fake Collection data (The "Software" settings)
        var collection = new PenumbraCollection { Id = "Col1", Name = "Test Collection" };
        collection.LocalSettings["ModA"] = new PenumbraMod { Id = "ModA", Name = "Winner Mod", IsEnabled = true, Priority = 10 };
        collection.LocalSettings["ModB"] = new PenumbraMod { Id = "ModB", Name = "Loser Mod", IsEnabled = true, Priority = 5 }; // Lower priority

        var fakeCache = new Dictionary<string, PenumbraCollection> { { "Col1", collection } };
        InjectMockCache(repo, fakeCache);

        // Act
        var state = repo.ComputeEffectiveState("Col1");

        // Assert
        Assert.Equal(1, state.ConflictModCount);
        Assert.Single(state.ConflictingMods);

        var loser = state.ConflictingMods[0];
        Assert.Equal("ModB", loser.Id);

        // Ensure the engine correctly identified who crushed this mod
        Assert.Contains("Winner Mod", loser.OverwrittenBy);

        // Ensure the engine correctly deduced the gear slot from "chara/weapon/w001.mdl"
        Assert.Contains("Weapon", loser.ConflictingSlots);
    }

    [Fact]
    public void ComputeEffectiveState_WithMultiOptionBitmask_ResolvesCorrectPaths() {
        // Arrange
        var repo = new PenumbraRepository(this.mockPluginLog, this.mockScannerManager);

        // 1. Setup Scanner Cache with a Multi Option Group
        var armoireGroup = new ArmoireOptionGroup { Type = "Multi" };
        armoireGroup.OptionPaths.Add(new HashSet<string> { "chara/equipment/e001/model/c0101e001_top.mdl" }); // Bit 0 (Value 1) - Body
        armoireGroup.OptionPaths.Add(new HashSet<string> { "chara/equipment/e001/model/c0101e001_met.mdl" }); // Bit 1 (Value 2) - Head

        var modEntry = new ArmoireModCacheEntry { ModName = "Option Mod" };
        modEntry.OptionGroups["Customization"] = armoireGroup;

        // We add a completely separate mod that alters the Head to force a conflict if the bitmask is evaluated correctly
        var enemyMod = new ArmoireModCacheEntry { ModName = "Enemy Hat Mod", ModifiedGamePaths = new HashSet<string> { "chara/equipment/e001/model/c0101e001_met.mdl" } };

        var fakeScannerCache = new Dictionary<string, ArmoireModCacheEntry> { { "ModOpt", modEntry }, { "Enemy", enemyMod } };
        this.mockScannerManager.ModCache.Returns(fakeScannerCache);

        // 2. Setup Collection data where the user selected Bit 1 (Value 2 = Head only)
        var collection = new PenumbraCollection { Id = "Col1", Name = "Test Collection" };
        var modWithSettings = new PenumbraMod { Id = "ModOpt", Name = "Option Mod", IsEnabled = true, Priority = 5 };
        modWithSettings.Settings["Customization"] = 2u; // Evaluates to the second option (Head)

        var enemySettings = new PenumbraMod { Id = "Enemy", Name = "Enemy Hat Mod", IsEnabled = true, Priority = 10 };

        collection.LocalSettings["ModOpt"] = modWithSettings;
        collection.LocalSettings["Enemy"] = enemySettings;

        InjectMockCache(repo, new Dictionary<string, PenumbraCollection> { { "Col1", collection } });

        // Act
        var state = repo.ComputeEffectiveState("Col1");

        // Assert
        Assert.Equal(1, state.ConflictModCount);

        // ModOpt should be marked as conflicting because its selected option (Head) collides with Enemy Hat Mod
        var loser = state.ConflictingMods[0];
        Assert.Equal("ModOpt", loser.Id);
        Assert.Contains("Head", loser.ConflictingSlots);
        Assert.DoesNotContain("Body", loser.ConflictingSlots); // Bit 0 was not selected, so no body conflict
    }
}