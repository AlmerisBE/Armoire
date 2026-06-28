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
using System;

namespace Armoire
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Armoire";

        private readonly IDalamudPluginInterface pluginInterface;
        private readonly ServiceProvider serviceProvider;
        private readonly CommandRegistry commandRegistry;
        private readonly MainWindow mainWindow;
        private readonly IWindowManager windowManager;

        public Plugin(
            IDalamudPluginInterface pluginInterface,
            IPluginLog pluginLog,
            ICommandManager commandManager)
        {
            // On sauvegarde l'interface pour pouvoir se désabonner dans le Dispose
            this.pluginInterface = pluginInterface;

            var services = new ServiceCollection();

            services.AddSingleton(pluginInterface);
            services.AddSingleton(pluginLog);
            services.AddSingleton(commandManager);

            services.AddSingleton<ConfigWindow>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<IWindowManager, WindowManager>();
            services.AddSingleton<CommandRegistry>();

            services.AddSingleton<IPenumbraClient, PenumbraClient>();
            services.AddSingleton<IPenumbraAnalyzer, PenumbraAnalyzer>();
            services.AddSingleton<PenumbraStatusPresenter>();
            services.AddSingleton<IUiComponent, PenumbraStatusView>();

            services.AddSingleton<IPluginCommand, MainCommand>();

            serviceProvider = services.BuildServiceProvider();

            mainWindow = serviceProvider.GetRequiredService<MainWindow>();

            // On récupère notre WindowManager
            windowManager = serviceProvider.GetRequiredService<IWindowManager>();

            var uiComponents = serviceProvider.GetServices<IUiComponent>();
            foreach (var component in uiComponents)
            {
                mainWindow.AttachComponent(component);
            }

            commandRegistry = serviceProvider.GetRequiredService<CommandRegistry>();
            commandRegistry.Initialize();

            // =========================================================
            // Connexion aux événements de l'UI de Dalamud
            // =========================================================
            this.pluginInterface.UiBuilder.Draw += this.windowManager.Draw;
            this.pluginInterface.UiBuilder.OpenMainUi += this.windowManager.ToggleMainWindow;
            this.pluginInterface.UiBuilder.OpenConfigUi += this.windowManager.ToggleConfigWindow;
        }

        public void Dispose()
        {
            // Toujours se désabonner proprement pour éviter les fuites de mémoire
            this.pluginInterface.UiBuilder.Draw -= this.windowManager.Draw;
            this.pluginInterface.UiBuilder.OpenMainUi -= this.windowManager.ToggleMainWindow;
            this.pluginInterface.UiBuilder.OpenConfigUi -= this.windowManager.ToggleConfigWindow;

            commandRegistry.Dispose();
            serviceProvider.Dispose();
        }
    }
}
