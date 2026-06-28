using Armoire.Core.Commands;
using Armoire.Core.UI;

namespace Armoire.Features.MainApp.Commands;

public class MainCommand : IPluginCommand {
    private readonly IWindowManager windowManager;

    public string Name => "/armoire";
    public string HelpMessage => "Opens the main Armoire interface.";

    public MainCommand(IWindowManager windowManager) {
        this.windowManager = windowManager;
    }

    public void Execute(string command, string args) {
        this.windowManager.ToggleMainWindow();
    }
}
