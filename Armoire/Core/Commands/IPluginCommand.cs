namespace Armoire.Core.Commands;

/// <summary>
/// Defines the structure of a chat command injected by a module.
/// </summary>
public interface IPluginCommand {
    /// <summary>
    /// Gets the command trigger (e.g., "/armoire").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the help message displayed in the /xlhelp menu.
    /// </summary>
    string HelpMessage { get; }

    /// <summary>
    /// Executes the logic associated with the command.
    /// </summary>
    /// <param name="command">The command string entered by the user.</param>
    /// <param name="args">The arguments passed after the command.</param>
    void Execute(string command, string args);
}
