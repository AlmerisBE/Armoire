namespace Armoire.Features.Penumbra.UI;

using Armoire.Core.UI;
using Dalamud.Bindings.ImGui;

public class PenumbraStatusView : IUiComponent {
    private readonly PenumbraStatusPresenter presenter;

    public PenumbraStatusView(PenumbraStatusPresenter presenter) {
        this.presenter = presenter;
    }

    public void Draw() {
        this.presenter.Tick();

        ImGui.TextDisabled("Penumbra Integration Status");
        ImGui.Spacing();

        ImGui.TextWrapped(presenter.CurrentReport);
        ImGui.Spacing();

        if (ImGui.Button("Refresh Status")) {
            presenter.RefreshReport();
        }
    }
}