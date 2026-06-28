using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System;

namespace Armoire.Features.MainApp.UI;

public class ConfigWindow : Window, IDisposable {
    public ConfigWindow() : base("Armoire - Configuration") {
        Size = new System.Numerics.Vector2(400, 300);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw() {
        ImGui.TextWrapped("Configuration options will be available here in the future.");
        ImGui.Spacing();
        ImGui.Separator();
    }

    public void Dispose() {
        // Cleaning future resources when necessary
    }
}
