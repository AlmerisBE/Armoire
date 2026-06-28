using Armoire.Features.Penumbra.Interfaces;

namespace Armoire.Features.Penumbra.Core;

public class PenumbraAnalyzer : IPenumbraAnalyzer {
    private readonly IPenumbraClient penumbraClient;

    public PenumbraAnalyzer(IPenumbraClient penumbraClient) {
        this.penumbraClient = penumbraClient;
    }

    public string GetStatusReport() {
        if (!this.penumbraClient.IsEnabled()) {
            return "Penumbra est hors ligne ou non installé.";
        }

        int modCount = this.penumbraClient.GetModsCount();
        return $"Penumbra est connecté. Mods installés : {modCount}";
    }
}
