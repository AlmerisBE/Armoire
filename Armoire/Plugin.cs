using Armoire.Core.Commands;
using Armoire.Core.DI;
using Armoire.Core.UI;
using Armoire.Features.ConflictEngine;
using Armoire.Features.MainApp.UI;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Armoire;

public sealed class Plugin : IDalamudPlugin {
    public string Name => "Armoire";

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ServiceProvider serviceProvider;
    private readonly CommandRegistry commandRegistry;
    private readonly MainWindow mainWindow;
    private readonly IWindowManager windowManager;
    private readonly IPenumbraSyncManager syncManager;
    private readonly ArmoireConfiguration configuration;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        IPluginLog pluginLog,
        ICommandManager commandManager,
        IObjectTable objectTable,
        IFramework framework,
        INotificationManager notificationManager,
        IClientState clientState,
        IDataManager dataManager,
        ITextureProvider textureProvider) {

        this.pluginInterface = pluginInterface;

        configuration = this.pluginInterface.GetPluginConfig() as ArmoireConfiguration ?? new ArmoireConfiguration();
        configuration.Initialize(this.pluginInterface);

        serviceProvider = ServiceConfigurator.ConfigureServices(
            pluginInterface,
            pluginLog,
            commandManager,
            objectTable,
            framework,
            notificationManager,
            clientState,
            dataManager,
            textureProvider,
            configuration
        );

        mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        windowManager = serviceProvider.GetRequiredService<IWindowManager>();
        syncManager = serviceProvider.GetRequiredService<IPenumbraSyncManager>();

        var uiComponents = serviceProvider.GetServices<IUiComponent>();
        foreach (var component in uiComponents) {
            mainWindow.AttachComponent(component);
        }

        commandRegistry = serviceProvider.GetRequiredService<CommandRegistry>();
        commandRegistry.Initialize();

        this.pluginInterface.UiBuilder.Draw += this.windowManager.Draw;
        this.pluginInterface.UiBuilder.OpenMainUi += this.windowManager.ToggleMainWindow;
        this.pluginInterface.UiBuilder.OpenConfigUi += this.windowManager.ToggleConfigWindow;
    }

    public void Dispose() {
        this.pluginInterface.UiBuilder.Draw -= this.windowManager.Draw;
        this.pluginInterface.UiBuilder.OpenMainUi -= this.windowManager.ToggleMainWindow;
        this.pluginInterface.UiBuilder.OpenConfigUi -= this.windowManager.ToggleConfigWindow;

        commandRegistry.Dispose();
        serviceProvider.Dispose();
    }
}
