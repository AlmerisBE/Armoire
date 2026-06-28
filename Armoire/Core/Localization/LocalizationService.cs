using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Armoire.Core.Localization;

public class LocalizationService : ILocalizationService
{
    private string currentLanguage;
    private readonly string localesDirectory;
    private Dictionary<string, string> currentTranslations;

    public LocalizationService(IRuntimeEnvironment runtime)
    {
        this.currentLanguage = runtime.GetClientLanguage();
        this.localesDirectory = Path.Combine(runtime.GetPluginDirectory(), "Locales");
        this.currentTranslations = new Dictionary<string, string>();

        LoadLanguage(this.currentLanguage);
    }

    public void SetLanguage(string languageCode)
    {
        if (this.currentLanguage != languageCode)
        {
            this.currentLanguage = languageCode;
            LoadLanguage(languageCode);
        }
    }

    public string GetString(string key)
    {
        if (this.currentTranslations.TryGetValue(key, out var localizedString))
        {
            return localizedString;
        }

        // Return the key directly if translation is missing, so it's obvious in the UI
        return key;
    }

    private void LoadLanguage(string languageCode)
    {
        var filePath = Path.Combine(this.localesDirectory, $"{languageCode}.json");

        if (File.Exists(filePath))
        {
            try
            {
                var jsonContent = File.ReadAllText(filePath);
                var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);

                if (translations != null)
                {
                    this.currentTranslations = translations;
                    return;
                }
            }
            catch (Exception)
            {
                // In a real scenario, we might want to log the JSON parsing error here
            }
        }

        // Fallback to empty if the file is missing or corrupt
        this.currentTranslations = new Dictionary<string, string>();
    }
}
