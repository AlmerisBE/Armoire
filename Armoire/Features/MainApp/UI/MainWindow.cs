using System;
using System.Collections.Generic;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Armoire.Core.UI;

namespace Armoire.Features.MainApp.UI
{
    public class MainWindow : Window, IDisposable
    {
        private readonly List<IUiComponent> attachedComponents;

        public event Action? OnConfigRequested;

        public IReadOnlyCollection<IUiComponent> AttachedComponents => attachedComponents;

        public MainWindow() : base("Armoire", ImGuiWindowFlags.NoCollapse)
        {
            attachedComponents = new List<IUiComponent>();
            Size = new System.Numerics.Vector2(600, 450);
            SizeCondition = ImGuiCond.FirstUseEver;
        }

        public void AttachComponent(IUiComponent component)
        {
            attachedComponents.Add(component);
        }

        public void InvokeConfigRequested()
        {
            OnConfigRequested?.Invoke();
        }

        public override void Draw()
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Welcome to Armoire!");

            ImGui.SameLine(ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X - 30);
            if (ImGui.Button(FontAwesomeIcon.Cog.ToIconString()))
            {
                OnConfigRequested?.Invoke();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            foreach (var component in attachedComponents)
            {
                component.Draw();
            }
        }

        public void Dispose()
        {
            foreach (var component in attachedComponents)
            {
                if (component is IDisposable disposableComponent)
                {
                    disposableComponent.Dispose();
                }
            }
            attachedComponents.Clear();
        }
    }
}
