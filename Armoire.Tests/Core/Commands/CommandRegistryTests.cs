using System.Collections.Generic;
using Xunit;
using NSubstitute;
using Dalamud.Plugin.Services;
using Dalamud.Game.Command;
using Armoire.Core.Commands;

namespace Armoire.Core.Tests.Commands
{
    public class CommandRegistryTests
    {
        [Fact]
        public void Initialize_ShouldRegisterAllCommandsWithDalamud()
        {
            // Arrange
            var mockCommandManager = Substitute.For<ICommandManager>();

            var mockCommand1 = Substitute.For<IPluginCommand>();
            mockCommand1.Name.Returns("/test1");
            mockCommand1.HelpMessage.Returns("Help 1");

            var mockCommand2 = Substitute.For<IPluginCommand>();
            mockCommand2.Name.Returns("/test2");
            mockCommand2.HelpMessage.Returns("Help 2");

            var commands = new List<IPluginCommand> { mockCommand1, mockCommand2 };
            var registry = new CommandRegistry(mockCommandManager, commands);

            // Act
            registry.Initialize();

            // Assert
            mockCommandManager.Received(1).AddHandler("/test1", Arg.Any<CommandInfo>());
            mockCommandManager.Received(1).AddHandler("/test2", Arg.Any<CommandInfo>());
        }

        [Fact]
        public void Dispose_ShouldRemoveAllRegisteredCommands()
        {
            // Arrange
            var mockCommandManager = Substitute.For<ICommandManager>();
            var mockCommand = Substitute.For<IPluginCommand>();
            mockCommand.Name.Returns("/test1");

            var registry = new CommandRegistry(mockCommandManager, new List<IPluginCommand> { mockCommand });
            registry.Initialize();

            // Act
            registry.Dispose();

            // Assert
            mockCommandManager.Received(1).RemoveHandler("/test1");
        }
    }
}
