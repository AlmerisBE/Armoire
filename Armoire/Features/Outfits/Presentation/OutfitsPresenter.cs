namespace Armoire.Features.Outfits.Presentation;

using Armoire.Core.Localization;
using Armoire.Features.ConflictEngine;
using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.GameData;
using Armoire.Features.GlamourerIpc;
using Armoire.Features.LocalScanner;
using Armoire.Features.Outfits;
using Armoire.Features.Outfits.Models;
using Armoire.Features.PenumbraIpc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

public class OutfitsPresenter : IOutfitsPresenter {
    private readonly IPenumbraSyncManager syncManager;
    private readonly IGlamourerClient glamourerClient;
    private readonly OutfitRepository repository;
    private EffectiveCollectionState? latestGlobalState;
    private readonly IModScannerManager scannerManager;
    private readonly IGameDataService gameDataService;
    private readonly ILocalizationService loc;

    // Directly expose the repository's read-only list
    public IReadOnlyList<ArmoireOutfit> Outfits => this.repository.Outfits;
    private readonly IPenumbraClient penumbraClient;

    public OutfitsPresenter(
        IPenumbraSyncManager syncManager,
        IGlamourerClient glamourerClient,
        OutfitRepository repository,
        IPenumbraClient penumbraClient,
        IModScannerManager scannerManager,
        IGameDataService gameDataService,
        ILocalizationService loc) {

        this.syncManager = syncManager;
        this.glamourerClient = glamourerClient;
        this.repository = repository;
        this.penumbraClient = penumbraClient;
        this.scannerManager = scannerManager;
        this.gameDataService = gameDataService;
        this.loc = loc;

        // Keep track of active mods via the sync manager event
        this.syncManager.OnStatusUpdated += (result) => {
            this.latestGlobalState = result?.GlobalState;
        };
    }

    public void CreateOutfit(string name) {
        string base64State = this.glamourerClient.GetCharacterStateBase64();
        string jsonState = this.glamourerClient.GetCharacterStateJson();

        if (string.IsNullOrEmpty(base64State) || string.IsNullOrEmpty(jsonState)) {
            return;
        }

        var newOutfit = new ArmoireOutfit {
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow,
            GlamourerBase64 = base64State
        };

        var personalBodyModelIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "e0000", "e0278", "e0279", "e9903"
        };

        var requiredModsSet = new Dictionary<string, OutfitModRequirement>();

        try {
            var root = Newtonsoft.Json.Linq.JObject.Parse(jsonState);

            foreach (var itemIdToken in root.SelectTokens("$..ItemId")) {
                uint itemId = itemIdToken.Value<uint>();
                if (itemId == 0) {
                    continue;
                }

                var itemInfo = this.gameDataService.GetItemInfo(itemId);
                string modelId = this.gameDataService.GetModelIdFromItemId(itemId);

                if (itemInfo != null && !string.IsNullOrEmpty(modelId)) {
                    var modifyingMods = new List<PenumbraMod>();
                    bool isPersonalOrInvisible = personalBodyModelIds.Contains(modelId);

                    // 1. Collect all mods touching this specific equipment piece
                    if (!isPersonalOrInvisible && this.latestGlobalState != null) {
                        foreach (var kvp in this.latestGlobalState.FileOwnership) {
                            string filePath = kvp.Key.ToLowerInvariant();
                            string expectedModelId = modelId.ToLowerInvariant();

                            if (filePath.Contains(expectedModelId)) {
                                bool isCorrectSlot = false;
                                string slotKey = itemInfo.SlotKey;

                                if (slotKey == "wpn" || slotKey == "sub") {
                                    isCorrectSlot = filePath.Contains("chara/weapon/");
                                } else if (slotKey == "rir" || slotKey == "ril") {
                                    isCorrectSlot = filePath.Contains("_rir.") || filePath.Contains("_rir_") || filePath.Contains("_ril.") || filePath.Contains("_ril_");
                                } else {
                                    isCorrectSlot = filePath.Contains($"_{slotKey}.") || filePath.Contains($"_{slotKey}_");
                                }

                                if (isCorrectSlot) {
                                    if (this.latestGlobalState.EffectiveMods.TryGetValue(kvp.Value, out var ownerMod)) {
                                        // Avoid adding duplicates to the initial pool
                                        if (!modifyingMods.Any(m => m.Id == ownerMod.Id)) {
                                            modifyingMods.Add(ownerMod);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // --- 2. THE ORPHAN CLEANUP HEURISTIC ---
                    var finalMods = new List<PenumbraMod>();
                    foreach (var mod in modifyingMods) {
                        // Is this mod overwritten by ANY OTHER mod that is ALSO touching this exact equipment piece?
                        bool isOrphan = modifyingMods.Any(other => mod.OverwrittenBy.Contains(other.Name));

                        // If it's a true winner (or a pure texture addition that didn't conflict), we keep it!
                        if (!isOrphan) {
                            finalMods.Add(mod);

                            // Add it to the global outfit requirements safely
                            if (!requiredModsSet.ContainsKey(mod.Id)) {
                                requiredModsSet[mod.Id] = new OutfitModRequirement {
                                    ModId = mod.Id,
                                    Name = mod.Name,
                                    Version = "1.0.0"
                                };
                            }
                        }
                    }

                    // 3. Save the clean list to the Inspection UI
                    newOutfit.Equipment.Add(new OutfitEquipmentPiece {
                        ItemId = itemId,
                        Name = itemInfo.Name,
                        IconId = itemInfo.IconId,
                        EquipLevel = itemInfo.EquipLevel,
                        ItemLevel = itemInfo.ItemLevel,
                        Category = itemInfo.SlotKey,
                        ModifyingModNames = finalMods.Count > 0 ? string.Join(", ", finalMods.Select(m => m.Name)) : this.loc.GetString("Outfits_VanillaMod")
                    });
                }
            }
        } catch { }

        // Finalize the requirement list with our perfectly filtered set
        newOutfit.RequiredMods = requiredModsSet.Values.ToList();
        this.repository.AddOutfit(newOutfit);
    }

    public void DeleteOutfit(Guid id) {
        this.repository.DeleteOutfit(id);
    }

    public void ActivateOutfit(ArmoireOutfit outfit) {
        if (outfit == null || string.IsNullOrEmpty(outfit.GlamourerBase64)) {
            return;
        }

        // 1. Apply the visual appearance via Glamourer
        bool success = this.glamourerClient.ApplyStateBase64(outfit.GlamourerBase64);

        if (success) {
            // TODO: Step 2 - Verify Penumbra mods. 
            // We need to check if the mods in outfit.RequiredMods are active 
            // and trigger warnings or automatic activations if they are missing/disabled.
        }
    }

    public (bool IsReady, List<string> MissingModNames) CheckOutfitReadiness(ArmoireOutfit outfit) {
        var missingMods = new List<string>();

        if (this.latestGlobalState == null) {
            return (false, new List<string> { this.loc.GetString("Outfits_WaitSync") });
        }

        if (outfit.RequiredMods == null) {
            return (true, missingMods);
        }

        foreach (var requirement in outfit.RequiredMods.Where(r => !r.IsIgnored)) {
            if (!this.latestGlobalState.EffectiveMods.TryGetValue(requirement.ModId, out var activeMod) || !activeMod.IsEnabled) {
                missingMods.Add(requirement.Name);
            }
        }

        return (missingMods.Count == 0, missingMods);
    }

    public IReadOnlyList<ModRequirementAnalysis> AnalyzeOutfitRequirements(IEnumerable<OutfitModRequirement> requirements) {
        var analysisList = new List<ModRequirementAnalysis>();

        // Fetch all physically installed mods directly from Penumbra
        var installedMods = this.penumbraClient.GetRawModsList();

        foreach (var req in requirements) {
            var analysis = new ModRequirementAnalysis { Requirement = req };

            // 1. Is it physically installed on the hard drive?
            // We check by ModId (directory name) or by exact Name as a fallback
            bool isInstalled = installedMods.ContainsKey(req.ModId) || installedMods.Values.Any(v => v == req.Name);

            if (!isInstalled) {
                analysis.Status = ModRequirementStatus.Missing;
                analysis.DetailMessage = this.loc.GetString("Outfits_StateNotInstalled");
                analysisList.Add(analysis);
                continue;
            }

            // 2. Is it active and conflict-free?
            if (this.latestGlobalState != null) {
                if (this.latestGlobalState.EffectiveMods.TryGetValue(req.ModId, out var activeMod) && activeMod.IsEnabled) {
                    bool hasConflict = this.latestGlobalState.ConflictingMods.Any(cm => cm.Id == req.ModId);

                    if (hasConflict) {
                        analysis.Status = ModRequirementStatus.DisabledOrConflicting;
                        analysis.DetailMessage = this.loc.GetString("Outfits_StateConflict");
                    } else {
                        analysis.Status = ModRequirementStatus.Ready;
                        analysis.DetailMessage = this.loc.GetString("Outfits_StateReady");
                    }
                } else {
                    analysis.Status = ModRequirementStatus.DisabledOrConflicting;
                    analysis.DetailMessage = this.loc.GetString("Outfits_StateDisabled");
                }
            } else {
                analysis.Status = ModRequirementStatus.DisabledOrConflicting;
                analysis.DetailMessage = this.loc.GetString("Outfits_StateUnknown");
            }

            analysisList.Add(analysis);
        }

        return analysisList;
    }

    public void ExportToClipboard(ArmoireOutfit outfit) {
        if (outfit == null) {
            return;
        }

        try {
            // 1. Serialize to a compact JSON string
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = false };
            string json = System.Text.Json.JsonSerializer.Serialize(outfit, options);

            // 2. Convert to UTF-8 bytes
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

            // 3. Compress using GZip (which natively encapsulates a CRC32 checksum)
            using var memoryStream = new MemoryStream();
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress)) {
                gzipStream.Write(jsonBytes, 0, jsonBytes.Length);
            }

            // 4. Encode the compressed binary payload to Base64
            string compressedBase64 = Convert.ToBase64String(memoryStream.ToArray());

            // We update the prefix token to reflect our new compressed V1 format
            Dalamud.Bindings.ImGui.ImGui.SetClipboardText($"armoire_v1:{compressedBase64}");
        } catch {
            // Suppress clipboard or OS encryption errors safely
        }
    }

    public void UpdateOutfit(ArmoireOutfit outfit) {
        this.repository.UpdateOutfit(outfit);
    }
}