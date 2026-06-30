namespace Armoire.Features.DiagnosticsUI.Presentation;

using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.ModDetails.Presentation;
using Armoire.Features.ModSwapper;
using Armoire.Features.PenumbraIpc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ConflictListPresenter : IConflictListPresenter {
    private readonly ArmoireConfiguration config;
    private readonly IModSwapperService swapperService;
    private readonly IPenumbraClient penumbraClient;
    private readonly IModDetailsPresenter modDetailsPresenter;

    public bool IsVisible { get; set; } = false;
    public string SearchQuery { get; set; } = string.Empty;

    private List<PenumbraMod> currentConflicts = new();

    public IReadOnlyDictionary<string, Armoire.Features.LocalScanner.Models.ModifiedModEntry> FilteredModifiedMods {
        get {
            if (this.config.ModifiedMods.Count == 0) {
                return new Dictionary<string, Armoire.Features.LocalScanner.Models.ModifiedModEntry>();
            }

            return this.config.ModifiedMods
                .Where(kvp => string.IsNullOrWhiteSpace(this.SearchQuery) ||
                              kvp.Value.ModName.Contains(this.SearchQuery, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }

    public IReadOnlyList<PenumbraMod> FilteredConflicts {
        get {
            return this.currentConflicts
                .Where(m => !this.config.ModifiedMods.ContainsKey(m.Id))
                .Where(m => string.IsNullOrWhiteSpace(this.SearchQuery) ||
                            m.Name.Contains(this.SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                            m.OverwrittenBy.Any(winner => winner.Contains(this.SearchQuery, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
    }

    public ConflictListPresenter(
        ArmoireConfiguration config,
        IModSwapperService swapperService,
        IPenumbraClient penumbraClient,
        IModDetailsPresenter modDetailsPresenter) {
        this.config = config;
        this.swapperService = swapperService;
        this.penumbraClient = penumbraClient;
        this.modDetailsPresenter = modDetailsPresenter;
    }

    public void Open() {
        this.IsVisible = true;
        this.SearchQuery = string.Empty;
    }

    public void UpdateLiveConflicts(List<PenumbraMod> liveConflicts) {
        this.currentConflicts = liveConflicts ?? new List<PenumbraMod>();
    }

    public void OpenModDetails(string modId) {
        this.modDetailsPresenter.Open(modId);
    }

    public bool IsModActivelyPatched(string modId) {
        string rootDir = this.penumbraClient.GetModDirectory();
        if (string.IsNullOrEmpty(rootDir)) {
            return false;
        }

        string fullModPath = Path.Combine(rootDir, modId);
        if (!Directory.Exists(fullModPath)) {
            return false;
        }

        var backupFiles = Directory.GetFiles(fullModPath, "*.armoire_bak", SearchOption.TopDirectoryOnly);
        return backupFiles.Length > 0;
    }

    public void ResetMod(string modId) {
        this.swapperService.ResetMod(modId);
    }

    public void RestoreMod(string modId, Dictionary<string, string> swaps) {
        foreach (var swapInfo in swaps) {
            // We pass an empty string for the texture provider fallback during a simple restore
            this.swapperService.PerformSwap(modId, swapInfo.Key, swapInfo.Value, string.Empty);
        }
    }
}