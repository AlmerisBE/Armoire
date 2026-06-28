namespace Armoire.Features.Penumbra.UI;

using Armoire.Features.Penumbra.Interfaces;
using Dalamud.Plugin.Services;
using System;

public class PenumbraStatusPresenter {
    private readonly IPenumbraAnalyzer analyzer;
    private readonly IPenumbraClient penumbraClient;
    private readonly IObjectTable objectTable;

    private string lastPlayerName = string.Empty;
    private int lastModCount = -1;
    private DateTime lastCheckTime = DateTime.MinValue;

    public string CurrentReport { get; private set; }

    public PenumbraStatusPresenter(IPenumbraAnalyzer analyzer, IPenumbraClient penumbraClient, IObjectTable objectTable) {
        this.analyzer = analyzer;
        this.penumbraClient = penumbraClient;
        this.objectTable = objectTable;
        this.CurrentReport = "Chargement des statistiques...";
    }

    public void Tick() {
        if ((DateTime.Now - lastCheckTime).TotalSeconds < 2.0) {
            return;
        }

        lastCheckTime = DateTime.Now;

        var currentPlayerName = this.objectTable.LocalPlayer?.Name.TextValue ?? string.Empty;
        var currentModCount = this.penumbraClient.GetModsCount();

        if (currentPlayerName != lastPlayerName || currentModCount != lastModCount) {
            lastPlayerName = currentPlayerName;
            lastModCount = currentModCount;
            RefreshReport();
        }
    }

    public void RefreshReport() {
        this.CurrentReport = this.analyzer.GetStatusReport();
    }
}