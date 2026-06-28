using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;

namespace Armoire.Core.Commands;

public class CommandRegistry : IDisposable {
    private readonly ICommandManager commandManager;
    private readonly IEnumerable<IPluginCommand> pluginCommands;

    public CommandRegistry(ICommandManager commandManager, IEnumerable<IPluginCommand> pluginCommands) {
        this.commandManager = commandManager;
        this.pluginCommands = pluginCommands;
    }

    public void Initialize() {
        foreach (var command in pluginCommands) {
            var commandInfo = new CommandInfo((cmd, args) => command.Execute(cmd, args)) {
                HelpMessage = command.HelpMessage
            };

            commandManager.AddHandler(command.Name, commandInfo);
        }
    }

    public void Dispose() {
        foreach (var command in pluginCommands) {
            commandManager.RemoveHandler(command.Name);
        }
    }
}
