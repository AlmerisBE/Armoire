namespace Armoire.Features.MainApp.UI;

using Armoire.Core.Localization;
using Armoire.Features.MainApp.Presentation;
using Armoire.Features.MainApp.UI.Tabs; // NEW: Tab namespaces
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;

public class MainWindow : Window, IDisposable {
    private readonly ILocalizationService loc;
    private readonly IMainWindowPresenter presenter;

    // Core structural sub-tabs view subcomponents
    private readonly HomeTab homeTab;
    private readonly ResolvedTab resolvedTab;
    private readonly StatsTab statsTab;
    private readonly ConfigTab configTab;
    private readonly AboutTab aboutTab;
    private readonly OutfitsTab outfitsTab;

    public event Action? OnConfigRequested;

    public MainWindow(
        ILocalizationService loc,
        IMainWindowPresenter presenter,
        HomeTab homeTab,
        ResolvedTab resolvedTab,
        StatsTab statsTab,
        ConfigTab configTab,
        AboutTab aboutTab,
        OutfitsTab outfitsTab) : base("Armoire") {
        this.loc = loc;
        this.presenter = presenter;
        this.homeTab = homeTab;
        this.resolvedTab = resolvedTab;
        this.statsTab = statsTab;
        this.configTab = configTab;
        this.aboutTab = aboutTab;

        Size = new Vector2(850, 600);
        SizeCondition = ImGuiCond.FirstUseEver;
        this.outfitsTab = outfitsTab;
    }

    public void InvokeConfigRequested() {
        OnConfigRequested?.Invoke();
    }

    public override void Draw() {
        // Grand header greeting the character
        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), string.Format(this.loc.GetString("Main_Welcome"), this.presenter.ConnectedCharacter));

        // Configuration shortcut gear top-right
        ImGui.SameLine(ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X - 30);
        if (ImGui.Button(Dalamud.Interface.FontAwesomeIcon.Cog.ToIconString())) {
            OnConfigRequested?.Invoke();
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        // Clean delegate drawing loop to dedicated single-intent classes
        if (ImGui.BeginTabBar("MainTabs")) {
            if (ImGui.BeginTabItem(this.loc.GetString("Main_TabHome"))) {
                this.homeTab.Draw();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(this.loc.GetString("Main_TabResolved"))) {
                this.resolvedTab.Draw();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(this.loc.GetString("Main_TabOutfits"))) {
                this.outfitsTab.Draw();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(this.loc.GetString("Main_TabStats"))) {
                this.statsTab.Draw();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(this.loc.GetString("Main_TabConfig"))) {
                this.configTab.Draw();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(this.loc.GetString("Main_TabAbout"))) {
                this.aboutTab.Draw();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    public void Dispose() { }
}