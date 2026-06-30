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
using Armoire.Features.ModDetails.UI;
using Armoire.Features.ModSwapper;
using Armoire.Features.PenumbraIpc;
using Armoire.Features.VanillaSearch;
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
        ITextureProvider textureProvider) {
        var services = new ServiceCollection();

        // 1. Dalamud native services
        services.AddSingleton(pluginInterface);
        services.AddSingleton(pluginLog);
        services.AddSingleton(commandManager);
        services.AddSingleton(objectTable);
        services.AddSingleton(framework);
        services.AddSingleton(notificationManager);
        services.AddSingleton(clientState);
        services.AddSingleton(dataManager);
        services.AddSingleton(textureProvider);

        // 2. Core services
        services.AddSingleton<IRuntimeEnvironment, RuntimeEnvironment>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IGameDataService, GameDataService>();
        services.AddSingleton<ConfigWindow>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<IWindowManager, WindowManager>();
        services.AddSingleton<CommandRegistry>();
        services.AddSingleton<IVanillaSearchService, VanillaSearchService>();
        services.AddSingleton<IModSwapperService, ModSwapperService>();

        // 3. Features dependencies (Penumbra, Glamourer)
        services.AddSingleton<IPenumbraClient, PenumbraClient>();
        services.AddSingleton<IPenumbraRepository, PenumbraRepository>();
        services.AddSingleton<IPenumbraAnalyzer, PenumbraAnalyzer>();
        services.AddSingleton<IPenumbraSyncManager, PenumbraSyncManager>();
        services.AddSingleton<IModScannerManager, ModScannerManager>();
        services.AddSingleton<IGlamourerClient, GlamourerClient>();

        // Section UI Penumbra
        services.AddSingleton<ModScannerWindow>();
        services.AddSingleton<ConflictListWindow>();
        services.AddSingleton<PenumbraStatusPresenter>();
        services.AddSingleton<IUiComponent, PenumbraStatusView>();
        services.AddSingleton<IModDetailsResolver, ModDetailsResolver>();
        services.AddSingleton<ModDetailsWindow>();
        services.AddSingleton<VanillaReplacementWindow>();

        // 4. Commands
        services.AddSingleton<IPluginCommand, MainCommand>();

        return services.BuildServiceProvider();
    }
}
