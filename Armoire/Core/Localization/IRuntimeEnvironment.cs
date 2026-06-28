namespace Armoire.Core.Localization;

/// <summary>
/// Provides information about the current execution environment, such as the game client's state and file paths.
/// </summary>
public interface IRuntimeEnvironment {
    /// <summary>
    /// Gets the current language code used by the game client (e.g., "En", "Fr", "De", "Ja").
    /// </summary>
    string GetClientLanguage();

    /// <summary>
    /// Gets the physical path to the plugin's installation directory.
    /// </summary>
    string GetPluginDirectory();
}
