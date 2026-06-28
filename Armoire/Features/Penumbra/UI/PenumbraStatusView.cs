using Armoire.Core.UI;
using Dalamud.Bindings.ImGui;

namespace Armoire.Features.Penumbra.UI;

public class PenumbraStatusView : IUiComponent {
    private readonly PenumbraStatusPresenter presenter;

    public PenumbraStatusView(PenumbraStatusPresenter presenter) {
        this.presenter = presenter;
    }

    public void Draw() {
        if (ImGui.CollapsingHeader("Penumbra Integration Status")) {
            ImGui.TextUnformatted("Current Status:");
            ImGui.Indent();
            ImGui.TextWrapped(presenter.CurrentReport);
            ImGui.Unindent();

            if (ImGui.Button("Refresh Status")) {
                presenter.RefreshReport();
            }
        }
    }
}
