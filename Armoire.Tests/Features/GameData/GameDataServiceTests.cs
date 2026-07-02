namespace Armoire.Tests.Features.GameData;

using Armoire.Core.Localization;
using Armoire.Features.GameData;
using Dalamud.Plugin.Services;
using NSubstitute;
using Xunit;

public class GameDataServiceTests {
    private readonly IDataManager mockDataManager;
    private readonly IPluginLog mockPluginLog;
    private readonly ILocalizationService mockLoc;

    public GameDataServiceTests() {
        // Initialize mocked dependencies
        this.mockDataManager = Substitute.For<IDataManager>();
        this.mockPluginLog = Substitute.For<IPluginLog>();
        this.mockLoc = Substitute.For<ILocalizationService>();

        // Set up the localization mock to return predictable strings for our assertions
        this.mockLoc.GetString("GameData_UnknownPath").Returns("Unknown Path");
        this.mockLoc.GetString("GameData_GenericEquip").Returns("Generic Equip {0}");
        this.mockLoc.GetString("GameData_GenericWeapon").Returns("Generic Weapon {0}");
        this.mockLoc.GetString("GameData_CustomFace").Returns("Face Customization");
        this.mockLoc.GetString("GameData_SystemUi").Returns("System UI");

        // Suffixes
        this.mockLoc.GetString("GameData_SuffixMdl").Returns(" (Model)");
        this.mockLoc.GetString("GameData_SuffixTex").Returns(" (Texture)");
        this.mockLoc.GetString("GameData_SuffixMtrl").Returns(" (Material)");
    }

    [Theory]
    [InlineData("chara/equipment/e0123/model/c0101e0123_top.mdl", "top", "Generic Equip e0123 (Model)")]
    [InlineData("chara/equipment/e0500/material/v0001/mt_c0101e0500_glv_a.mtrl", "glv", "Generic Equip e0500 (Material)")]
    [InlineData("chara/equipment/e0999/texture/v01_c0101e0999_sho_n.tex", "sho", "Generic Equip e0999 (Texture)")]
    public void ResolveItem_WithEquipmentPaths_ExtractsSlotAndModelCorrectly(string path, string expectedSlot, string expectedName) {
        // Arrange
        var service = new GameDataService(this.mockDataManager, this.mockPluginLog, this.mockLoc);

        // Act
        var result = service.ResolveItem(path);

        // Assert
        Assert.Equal(expectedSlot, result.SlotKey);
        Assert.Equal(expectedName, result.Name);
    }

    [Fact]
    public void ResolveItem_WithWeaponPath_ExtractsWeaponSlotCorrectly() {
        // Arrange
        var service = new GameDataService(this.mockDataManager, this.mockPluginLog, this.mockLoc);
        var path = "chara/weapon/w0050/model/w0050b0001.mdl";

        // Act
        var result = service.ResolveItem(path);

        // Assert
        // Weapons default to "wpn" when not found in the Lumina cache
        Assert.Equal("wpn", result.SlotKey);
        Assert.Equal("Generic Weapon w0050 (Model)", result.Name);
    }

    [Fact]
    public void ResolveItem_WithCustomizationPath_ExtractsCustomSlotCorrectly() {
        // Arrange
        var service = new GameDataService(this.mockDataManager, this.mockPluginLog, this.mockLoc);
        var path = "chara/human/c0101/obj/face/f0001/model/c0101f0001_fac.mdl";

        // Act
        var result = service.ResolveItem(path);

        // Assert
        Assert.Equal("custom", result.SlotKey);
        Assert.Equal("Face Customization (Model)", result.Name);
    }

    [Fact]
    public void ResolveItem_WithSystemPath_ReturnsUnknownSlot() {
        // Arrange
        var service = new GameDataService(this.mockDataManager, this.mockPluginLog, this.mockLoc);
        var path = "ui/icon/000000/000001.tex";

        // Act
        var result = service.ResolveItem(path);

        // Assert
        Assert.Equal("unknown", result.SlotKey);
        Assert.Equal("System UI (Texture)", result.Name);
    }

    [Fact]
    public void ResolveItem_WithNullOrEmptyPath_ReturnsSafeFallback() {
        // Arrange
        var service = new GameDataService(this.mockDataManager, this.mockPluginLog, this.mockLoc);

        // Act
        var result = service.ResolveItem(string.Empty);

        // Assert
        // Ensure the service does not throw an ArgumentNullException
        Assert.Equal("unknown", result.SlotKey);
        Assert.Equal("Unknown Path", result.Name);
    }
}