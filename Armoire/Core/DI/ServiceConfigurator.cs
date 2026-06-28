using Armoire.Core.Commands;
using Armoire.Core.UI;
using Armoire.Features.MainApp.Commands;
using Armoire.Features.MainApp.UI;
using Armoire.Features.Penumbra.Core;
using Armoire.Features.Penumbra.Interfaces;
using Armoire.Features.Penumbra.UI;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Armoire.Core.DI;

public static class ServiceConfigurator {
    public static ServiceProvider ConfigureServices(
        IDalamudPluginInterface pluginInterface,
        IPluginLog pluginLog,
        ICommandManager commandManager) {
        var services = new ServiceCollection();

        // 1. Dalamud native services
        services.AddSingleton(pluginInterface);
        services.AddSingleton(pluginLog);
        services.AddSingleton(commandManager);

        // 2. Core services
        services.AddSingleton<ConfigWindow>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<IWindowManager, WindowManager>();
        services.AddSingleton<CommandRegistry>();

        // 3. Features dependencies (Penumbra)
        services.AddSingleton<IPenumbraClient, PenumbraClient>();
        services.AddSingleton<IPenumbraAnalyzer, PenumbraAnalyzer>();
        services.AddSingleton<PenumbraStatusPresenter>();
        services.AddSingleton<IUiComponent, PenumbraStatusView>();

        // 4. Commands
        services.AddSingleton<IPluginCommand, MainCommand>();

        return services.BuildServiceProvider();
    }
}
