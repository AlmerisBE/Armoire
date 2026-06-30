namespace Armoire.Features.MainApp.Presentation;

using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.DiagnosticsUI.Presentation;
using Armoire.Features.ModDetails.Presentation;
using Armoire.Features.PenumbraIpc;
using Dalamud.Plugin;
using Dalamud.Utility;
using System.Collections.Generic;
using System.Linq;

public class MainWindowPresenter : IMainWindowPresenter {
    private readonly IPenumbraStatusPresenter statusPresenter;
    private readonly IConflictListPresenter conflictPresenter;
    private readonly IModDetailsPresenter modDetailsPresenter;
    private readonly IPenumbraClient penumbraClient;
    private readonly ArmoireConfiguration configuration;
    private readonly IDalamudPluginInterface pluginInterface;

    public string ConnectedCharacter => this.statusPresenter.ConnectedCharacter;
    public int TotalInstalledMods => this.statusPresenter.TotalIpcMods;
    public int ConfiguredMods => this.statusPresenter.ConfiguredMods;
    public int ActiveMods => this.statusPresenter.EnabledMods;

    public string MainCollectionName => this.statusPresenter.ActiveCollections.FirstOrDefault()?.Name ?? "Défaut";

    public int IgnoredConflictsCount => this.conflictPresenter.FilteredConflicts.Count;
    public int ResolvedConflictsCount => this.configuration.ModifiedMods.Count;

    public string PenumbraModDirectory => this.penumbraClient.GetModDirectory();
    public string PluginVersion => this.pluginInterface.Manifest.AssemblyVersion.ToString();
    public string PluginAuthor => this.pluginInterface.Manifest.Author;
    public IReadOnlyList<PenumbraMod> CurrentConflicts => this.conflictPresenter.FilteredConflicts;

    public string ConflictSearchQuery {
        get => this.conflictPresenter.SearchQuery;
        set => this.conflictPresenter.SearchQuery = value;
    }

    public IReadOnlyDictionary<string, ModifiedModEntry> ResolvedMods => this.conflictPresenter.FilteredModifiedMods;

    public MainWindowPresenter(
        IPenumbraStatusPresenter statusPresenter,
        IConflictListPresenter conflictPresenter,
        IModDetailsPresenter modDetailsPresenter,
        IPenumbraClient penumbraClient,
        ArmoireConfiguration configuration,
        IDalamudPluginInterface pluginInterface) {
        this.statusPresenter = statusPresenter;
        this.conflictPresenter = conflictPresenter;
        this.modDetailsPresenter = modDetailsPresenter;
        this.penumbraClient = penumbraClient;
        this.configuration = configuration;
        this.pluginInterface = pluginInterface;
    }

    public void OpenConflictResolution(string modId) {
        this.modDetailsPresenter.Open(modId);
    }

    public void OpenDiscord() {
        // Replace with your actual Discord invite link
        Util.OpenLink("https://discord.gg/your-invite-link");
    }

    public bool IsModActivelyPatched(string modId) => this.conflictPresenter.IsModActivelyPatched(modId);
    public void ResetMod(string modId) => this.conflictPresenter.ResetMod(modId);
    public void RestoreMod(string modId, Dictionary<string, string> swaps) => this.conflictPresenter.RestoreMod(modId, swaps);
}