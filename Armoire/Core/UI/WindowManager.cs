namespace Armoire.Core.UI;

using Armoire.Features.DiagnosticsUI;
using Armoire.Features.MainApp.UI;
using Armoire.Features.ModDetails.UI;
using Armoire.Features.Outfits.UI;
using Armoire.Features.VanillaSearch.UI;
using Dalamud.Interface.Windowing;
using System;

public class WindowManager : IWindowManager, IDisposable {
    private readonly MainWindow mainWindow;
    private readonly ConfigWindow configWindow;

    private readonly ModDetailsWindow modDetailsWindow;
    private readonly VanillaReplacementWindow vanillaReplacementWindow;
    private readonly ModScannerWindow modScannerWindow;
    private readonly OutfitDetailsWindow outfitDetailsWindow;

    private readonly WindowSystem windowSystem;

    public WindowManager(
        MainWindow mainWindow,
        ConfigWindow configWindow,
        ModDetailsWindow modDetailsWindow,
        VanillaReplacementWindow vanillaReplacementWindow,
        ModScannerWindow modScannerWindow,
        OutfitDetailsWindow outfitDetailsWindow) {
        this.mainWindow = mainWindow;
        this.configWindow = configWindow;
        this.modDetailsWindow = modDetailsWindow;
        this.vanillaReplacementWindow = vanillaReplacementWindow;
        this.modScannerWindow = modScannerWindow;
        this.outfitDetailsWindow = outfitDetailsWindow;

        this.windowSystem = new WindowSystem("ArmoireWindowSystem");
        this.windowSystem.AddWindow(this.mainWindow);
        this.windowSystem.AddWindow(this.configWindow);

        this.mainWindow.OnConfigRequested += ToggleConfigWindow;
    }

    public void ToggleMainWindow() => mainWindow.IsOpen = !mainWindow.IsOpen;
    public void ToggleConfigWindow() => configWindow.IsOpen = !configWindow.IsOpen;

    public void Draw() {
        // 1. Draw standard Dalamud WindowSystem windows (Main & Config)
        this.windowSystem.Draw();

        // 2. Draw independent MVP views
        this.modDetailsWindow.Draw();
        this.vanillaReplacementWindow.Draw();
        this.modScannerWindow.Draw();
        this.outfitDetailsWindow.Draw();
    }

    public void Dispose() {
        this.mainWindow.OnConfigRequested -= ToggleConfigWindow;
        this.windowSystem.RemoveAllWindows();

        if (this.mainWindow is IDisposable dm) {
            dm.Dispose();
        }

        if (this.configWindow is IDisposable dc) {
            dc.Dispose();
        }
    }
}