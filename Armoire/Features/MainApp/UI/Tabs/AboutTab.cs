namespace Armoire.Features.MainApp.UI.Tabs;

using Armoire.Core.Localization;
using Armoire.Features.MainApp.Presentation;
using Dalamud.Bindings.ImGui;
using System.Numerics;

public class AboutTab {
    private readonly ILocalizationService loc;
    private readonly IMainWindowPresenter presenter;

    public AboutTab(ILocalizationService loc, IMainWindowPresenter presenter) {
        this.loc = loc;
        this.presenter = presenter;
    }

    public void Draw() {
        ImGui.Spacing();

        ImGui.TextUnformatted(string.Format(this.loc.GetString("Main_AboutOrigin"), this.presenter.PluginAuthor));
        ImGui.TextDisabled(string.Format(this.loc.GetString("Main_AboutVersion"), this.presenter.PluginVersion));

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.8f, 1.0f), this.loc.GetString("Main_AboutThanks"));
        ImGui.Spacing();

        if (ImGui.Button(this.loc.GetString("Main_AboutDiscordBtn"))) {
            this.presenter.OpenDiscord();
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextDisabled(this.loc.GetString("Main_AboutHistory"));
    }
}