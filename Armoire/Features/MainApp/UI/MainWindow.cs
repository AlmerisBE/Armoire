namespace Armoire.Features.MainApp.UI;

using Armoire.Core.Localization;
using Armoire.Core.UI;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;

public class MainWindow : Window, IDisposable {
    private readonly List<IUiComponent> attachedComponents;
    private readonly ILocalizationService loc;

    public event Action? OnConfigRequested;
    public IReadOnlyCollection<IUiComponent> AttachedComponents => attachedComponents;

    public MainWindow(ILocalizationService localizationService) : base("Armoire", ImGuiWindowFlags.NoCollapse) {
        this.loc = localizationService;
        this.attachedComponents = new List<IUiComponent>();
        Size = new System.Numerics.Vector2(600, 450);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void AttachComponent(IUiComponent component) {
        this.attachedComponents.Add(component);
    }

    public void InvokeConfigRequested() {
        OnConfigRequested?.Invoke();
    }

    public override void Draw() {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(this.loc.GetString("MainWindow_Welcome"));

        ImGui.SameLine(ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X - 30);
        if (ImGui.Button(FontAwesomeIcon.Cog.ToIconString())) {
            OnConfigRequested?.Invoke();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        foreach (var component in this.attachedComponents) {
            component.Draw();
        }
    }

    public void Dispose() {
        foreach (var component in this.attachedComponents) {
            if (component is IDisposable disposableComponent) {
                disposableComponent.Dispose();
            }
        }
        this.attachedComponents.Clear();
    }
}