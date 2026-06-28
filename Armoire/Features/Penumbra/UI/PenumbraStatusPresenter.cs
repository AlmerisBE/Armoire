namespace Armoire.Features.Penumbra.UI;

using Armoire.Features.Penumbra.Core.Models;
using Armoire.Features.Penumbra.Interfaces;
using Dalamud.Plugin.Services;
using System;
using System.Threading.Tasks;

public class PenumbraStatusPresenter {
    private readonly IPenumbraAnalyzer analyzer;
    private readonly IPenumbraClient penumbraClient;
    private readonly IPenumbraRepository repository;
    private readonly IObjectTable objectTable;

    private string lastPlayerName = string.Empty;
    private int lastModCount = -1;
    private DateTime lastCheckTime = DateTime.MinValue;

    // Le fameux drapeau pour synchroniser avec le Thread Principal
    private bool pendingRefreshSemaphore = false;

    public PenumbraStatusResult CurrentStatus { get; private set; }

    public PenumbraStatusPresenter(IPenumbraAnalyzer analyzer, IPenumbraClient penumbraClient, IPenumbraRepository repository, IObjectTable objectTable) {
        this.analyzer = analyzer;
        this.penumbraClient = penumbraClient;
        this.repository = repository;
        this.objectTable = objectTable;
        this.CurrentStatus = new PenumbraStatusResult();
    }

    public void Tick() {
        // 1. Exécution des tâches en attente sur le THREAD PRINCIPAL
        if (pendingRefreshSemaphore) {
            RefreshReport();
            pendingRefreshSemaphore = false;
        }

        // 2. On bloque les nouvelles requêtes si le disque tourne encore
        if (this.repository.IsLoading) {
            return;
        }

        // 3. Limite de vérification (2 secondes)
        if ((DateTime.Now - lastCheckTime).TotalSeconds < 2.0) {
            return;
        }

        lastCheckTime = DateTime.Now;

        // Lecture de l'état du jeu (Sécurisé : nous sommes sur le Thread Principal ici)
        var currentPlayerName = this.objectTable.LocalPlayer?.Name.TextValue ?? string.Empty;
        var currentModCount = this.penumbraClient.GetModsCount();

        bool hasChanged = currentPlayerName != lastPlayerName || currentModCount != lastModCount;
        bool needsRetry = this.CurrentStatus.ActiveCollections.Count == 0
                          && this.penumbraClient.IsEnabled()
                          && !string.IsNullOrEmpty(currentPlayerName);

        if (hasChanged || needsRetry) {
            lastPlayerName = currentPlayerName;
            lastModCount = currentModCount;

            // 4. Lancement asynchrone de la lecture disque
            _ = Task.Run(async () => {
                await this.repository.SyncDataAsync();

                // Au lieu d'appeler l'UI ici (ce qui crasherait), on lève le drapeau !
                pendingRefreshSemaphore = true;
            });
        }
    }

    public void RefreshReport() {
        this.CurrentStatus = this.analyzer.GetStatusReport();
    }
}