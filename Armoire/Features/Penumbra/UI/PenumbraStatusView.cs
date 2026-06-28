using Armoire.Core.UI;
using Dalamud.Bindings.ImGui;

namespace Armoire.Features.Penumbra.UI;

public class PenumbraStatusView : IUiComponent {
    private readonly PenumbraStatusPresenter presenter;

    public PenumbraStatusView(PenumbraStatusPresenter presenter) {
        this.presenter = presenter;
    }

    public void Draw() {
        ImGui.TextDisabled("Penumbra Integration Status");
        ImGui.Spacing();

        ImGui.TextWrapped(presenter.CurrentReport);
        ImGui.Spacing();

        if (ImGui.Button("Refresh Status")) {
            presenter.RefreshReport();
        }
    }
}