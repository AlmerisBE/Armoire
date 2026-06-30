using Armoire.Core.Commands;
using Armoire.Core.Localization;
using Armoire.Core.UI;
using Armoire.Features.ConflictEngine;
using Armoire.Features.DiagnosticsUI;
using Armoire.Features.GameData;
using Armoire.Features.GlamourerIpc;
using Armoire.Features.LocalScanner;
using Armoire.Features.MainApp.Commands;
using Armoire.Features.MainApp.UI;
using Armoire.Features.ModDetails;
using Armoire.Features.ModDetails.Presentation;
using Armoire.Features.ModDetails.UI;
using Armoire.Features.ModSwapper;
using Armoire.Features.PenumbraIpc;
using Armoire.Features.VanillaSearch;
using Armoire.Features.VanillaSearch.Presentation;
using Armoire.Features.VanillaSearch.UI;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Armoire.Core.DI;

public static class ServiceConfigurator {

    public static ServiceProvider ConfigureServices(
        IDalamudPluginInterface pluginInterface,
        IPluginLog pluginLog,
        ICommandManager commandManager,
        IObjectTable objectTable,
        IFramework framework,
        INotificationManager notificationManager,
        IClientState clientState,
        IDataManager dataManager,
        ITextureProvider textureProvider,
        ArmoireConfiguration configuration) {
        var services = new ServiceCollection();

        // The main configuration acts as a clean, readable table of contents
        RegisterDalamudServices(services, pluginInterface, pluginLog, commandManager, objectTable, framework, notificationManager, clientState, dataManager, textureProvider);
        RegisterCoreInfrastructure(services, configuration);
        RegisterExternalIntegrations(services);
        RegisterFeatureServices(services);
        RegisterPresenters(services);
        RegisterUiComponents(services);
        RegisterCommands(services);

        return services.BuildServiceProvider();
    }

    private static void RegisterDalamudServices(
        IServiceCollection services,
        IDalamudPluginInterface pluginInterface,
        IPluginLog pluginLog,
        ICommandManager commandManager,
        IObjectTable objectTable,
        IFramework framework,
        INotificationManager notificationManager,
        IClientState clientState,
        IDataManager dataManager,
        ITextureProvider textureProvider) {
        services.AddSingleton(pluginInterface);
        services.AddSingleton(pluginLog);
        services.AddSingleton(commandManager);
        services.AddSingleton(objectTable);
        services.AddSingleton(framework);
        services.AddSingleton(notificationManager);
        services.AddSingleton(clientState);
        services.AddSingleton(dataManager);
        services.AddSingleton(textureProvider);
    }

    private static void RegisterCoreInfrastructure(IServiceCollection services, ArmoireConfiguration configuration) {
        services.AddSingleton(configuration);
        services.AddSingleton<IRuntimeEnvironment, RuntimeEnvironment>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IWindowManager, WindowManager>();
        services.AddSingleton<CommandRegistry>();
    }

    private static void RegisterExternalIntegrations(IServiceCollection services) {
        services.AddSingleton<IPenumbraClient, PenumbraClient>();
        services.AddSingleton<IGlamourerClient, GlamourerClient>();
    }

    private static void RegisterFeatureServices(IServiceCollection services) {
        services.AddSingleton<IGameDataService, GameDataService>();
        services.AddSingleton<IPenumbraRepository, PenumbraRepository>();
        services.AddSingleton<IPenumbraAnalyzer, PenumbraAnalyzer>();
        services.AddSingleton<IPenumbraSyncManager, PenumbraSyncManager>();
        services.AddSingleton<IModScannerManager, ModScannerManager>();
        services.AddSingleton<IModDetailsResolver, ModDetailsResolver>();
        services.AddSingleton<IVanillaSearchService, VanillaSearchService>();
        services.AddSingleton<IModSwapperService, ModSwapperService>();
    }

    private static void RegisterPresenters(IServiceCollection services) {
        services.AddSingleton<IModDetailsPresenter, ModDetailsPresenter>();
        services.AddSingleton<IVanillaReplacementPresenter, VanillaReplacementPresenter>();
        services.AddSingleton<PenumbraStatusPresenter>();
    }

    private static void RegisterUiComponents(IServiceCollection services) {
        services.AddSingleton<ConfigWindow>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<ModScannerWindow>();
        services.AddSingleton<ConflictListWindow>();
        services.AddSingleton<ModDetailsWindow>();
        services.AddSingleton<VanillaReplacementWindow>();
        services.AddSingleton<IUiComponent, PenumbraStatusView>();
    }

    private static void RegisterCommands(IServiceCollection services) {
        services.AddSingleton<IPluginCommand, MainCommand>();
    }
}