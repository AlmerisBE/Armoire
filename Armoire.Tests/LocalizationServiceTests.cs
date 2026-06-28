using Armoire.Core.Localization;
using NSubstitute;
using Xunit;

namespace Armoire.Tests;

public class LocalizationServiceTests : IDisposable
{
    private readonly string tempDirectory;
    private readonly string localesDirectory;
    private readonly IRuntimeEnvironment mockRuntime;

    public LocalizationServiceTests()
    {
        // 1. ARRANGE (Global Setup)
        // Create a unique temporary directory for this specific test run
        this.tempDirectory = Path.Combine(Path.GetTempPath(), "ArmoireTests_" + Guid.NewGuid().ToString());
        this.localesDirectory = Path.Combine(this.tempDirectory, "Locales");
        Directory.CreateDirectory(this.localesDirectory);

        // Create dummy JSON files for English and French
        var enJson = "{\"TestKey\": \"TestValueEN\", \"OnlyEnKey\": \"OnlyEN\"}";
        File.WriteAllText(Path.Combine(this.localesDirectory, "En.json"), enJson);

        var frJson = "{\"TestKey\": \"TestValueFR\"}";
        File.WriteAllText(Path.Combine(this.localesDirectory, "Fr.json"), frJson);

        // Configure the mocked environment to return our temporary directory
        this.mockRuntime = Substitute.For<IRuntimeEnvironment>();
        this.mockRuntime.GetPluginDirectory().Returns(this.tempDirectory);
    }

    public void Dispose()
    {
        // CLEANUP
        // Delete the temporary directory and all its contents after each test
        if (Directory.Exists(this.tempDirectory))
        {
            Directory.Delete(this.tempDirectory, true);
        }
    }

    [Fact]
    public void GetString_WhenKeyExists_ReturnsTranslatedString()
    {
        // Arrange
        this.mockRuntime.GetClientLanguage().Returns("En");
        var service = new LocalizationService(this.mockRuntime);

        // Act
        string result = service.GetString("TestKey");

        // Assert
        Assert.Equal("TestValueEN", result);
    }

    [Fact]
    public void GetString_WhenKeyIsMissing_ReturnsTheKeyItself()
    {
        // Arrange
        this.mockRuntime.GetClientLanguage().Returns("En");
        var service = new LocalizationService(this.mockRuntime);

        // Act
        string result = service.GetString("UnknownKey");

        // Assert
        Assert.Equal("UnknownKey", result);
    }

    [Fact]
    public void SetLanguage_WhenChangingLanguage_UpdatesTranslations()
    {
        // Arrange
        this.mockRuntime.GetClientLanguage().Returns("En");
        var service = new LocalizationService(this.mockRuntime);

        // Act
        service.SetLanguage("Fr");
        string result = service.GetString("TestKey");

        // Assert
        Assert.Equal("TestValueFR", result);
    }

    [Fact]
    public void GetString_WhenFileIsMissing_ReturnsTheKeyItself()
    {
        // Arrange
        // "De" json file was never created in our constructor
        this.mockRuntime.GetClientLanguage().Returns("De");
        var service = new LocalizationService(this.mockRuntime);

        // Act
        string result = service.GetString("TestKey");

        // Assert
        Assert.Equal("TestKey", result);
    }
}
