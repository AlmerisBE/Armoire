using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Armoire.Core.Localization;

public class RuntimeEnvironment : IRuntimeEnvironment
{
    private readonly IClientState clientState;
    private readonly IDalamudPluginInterface pluginInterface;

    public RuntimeEnvironment(IClientState clientState, IDalamudPluginInterface pluginInterface)
    {
        this.clientState = clientState;
        this.pluginInterface = pluginInterface;
    }

    public string GetClientLanguage()
    {
        return this.clientState.ClientLanguage switch
        {
            Dalamud.Game.ClientLanguage.French => "Fr",
            Dalamud.Game.ClientLanguage.German => "De",
            Dalamud.Game.ClientLanguage.Japanese => "Ja",
            _ => "En"
        };
    }

    public string GetPluginDirectory()
    {
        // Returns the folder where Armoire.dll is executed from
        return this.pluginInterface.AssemblyLocation.Directory?.FullName ?? string.Empty;
    }
}
