namespace Armoire.Features.MainApp.UI.Tabs;

using Armoire.Core.Localization;
using Armoire.Features.MainApp.Presentation;
using Dalamud.Bindings.ImGui;
using System.Numerics;

public class ConfigTab {
    private readonly ILocalizationService loc;
    private readonly IMainWindowPresenter presenter;

    public ConfigTab(ILocalizationService loc, IMainWindowPresenter presenter) {
        this.loc = loc;
        this.presenter = presenter;
    }

    public void Draw() {
        ImGui.Spacing();
        ImGui.TextUnformatted(this.loc.GetString("Main_ConfigPenumbraDir"));
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1.0f), this.presenter.PenumbraModDirectory);

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        ImGui.TextDisabled(this.loc.GetString("Main_ConfigFuture"));
    }
}