namespace Armoire.Features.Penumbra.UI;

using Armoire.Features.Penumbra.Core.Models;
using Armoire.Features.Penumbra.Interfaces;
using System;

public class PenumbraStatusPresenter : IDisposable {
    private readonly IPenumbraSyncManager syncManager;

    public PenumbraStatusResult CurrentStatus { get; private set; }

    public PenumbraStatusPresenter(IPenumbraSyncManager syncManager) {
        this.syncManager = syncManager;
        this.CurrentStatus = new PenumbraStatusResult();

        this.syncManager.OnStatusUpdated += UpdateStatus;
    }

    private void UpdateStatus(PenumbraStatusResult newStatus) {
        this.CurrentStatus = newStatus;
    }

    public void RefreshReport() {
        this.syncManager.ForceRefresh();
    }

    public void Dispose() {
        this.syncManager.OnStatusUpdated -= UpdateStatus;
    }
}