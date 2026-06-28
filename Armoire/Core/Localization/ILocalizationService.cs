namespace Armoire.Core.Localization;

/// <summary>
/// Manages translations and provides localized strings for the user interface.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Sets the active language for the plugin.
    /// </summary>
    void SetLanguage(string languageCode);

    /// <summary>
    /// Retrieves the localized string associated with the specified key.
    /// </summary>
    string GetString(string key);
}
