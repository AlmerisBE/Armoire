using System;
using Armoire.Features.MainApp.UI;
using Dalamud.Interface.Windowing;

namespace Armoire.Core.UI
{
    public class WindowManager : IWindowManager, IDisposable
    {
        private readonly MainWindow mainWindow;
        private readonly ConfigWindow configWindow;
        private readonly WindowSystem windowSystem;

        public WindowManager(MainWindow mainWindow, ConfigWindow configWindow)
        {
            this.mainWindow = mainWindow;
            this.configWindow = configWindow;

            windowSystem = new WindowSystem("ArmoireWindowSystem");
            windowSystem.AddWindow(this.mainWindow);
            windowSystem.AddWindow(this.configWindow);

            this.mainWindow.OnConfigRequested += ToggleConfigWindow;
        }

        public void ToggleMainWindow()
        {
            mainWindow.IsOpen = !mainWindow.IsOpen;
        }

        public void ToggleConfigWindow()
        {
            configWindow.IsOpen = !configWindow.IsOpen;
        }

        public void Draw()
        {
            windowSystem.Draw();
        }

        public void Dispose()
        {
            mainWindow.OnConfigRequested -= ToggleConfigWindow;

            windowSystem.RemoveAllWindows();
            mainWindow.Dispose();
            configWindow.Dispose();
        }
    }
}
