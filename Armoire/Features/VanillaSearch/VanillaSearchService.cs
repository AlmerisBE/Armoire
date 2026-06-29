namespace Armoire.Features.VanillaSearch;

using Armoire.Features.ConflictEngine.Models;
using Armoire.Features.LocalScanner;
using Armoire.Features.VanillaSearch.Models;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class VanillaSearchService : IVanillaSearchService {
    private readonly IDataManager dataManager;
    private readonly IModScannerManager scannerManager;

    // Caches for fast cross-referencing
    private readonly HashSet<uint> craftedItemIds = [];

    public VanillaSearchService(IDataManager dataManager, IModScannerManager scannerManager) {
        this.dataManager = dataManager;
        this.scannerManager = scannerManager;

        InitializeMetadataCaches();
    }

    private void InitializeMetadataCaches() {
        var recipeSheet = this.dataManager.GetExcelSheet<Recipe>();
        if (recipeSheet != null) {
            foreach (var recipe in recipeSheet) {
                if (recipe.ItemResult.RowId > 0) {
                    this.craftedItemIds.Add(recipe.ItemResult.RowId);
                }
            }
        }
    }

    public List<VanillaItem> GetAvailableReplacements(string slotKey, EffectiveCollectionState globalState) {
        var results = new List<VanillaItem>();
        var itemSheet = this.dataManager.GetExcelSheet<Item>();
        if (itemSheet == null) {
            return results;
        }

        var blockedModelIds = GetModifiedModelIds(slotKey, globalState);

        foreach (var item in itemSheet) {
            if (item.ModelMain == 0 || string.IsNullOrEmpty(item.Name.ExtractText())) {
                continue;
            }

            var slot = item.EquipSlotCategory.Value;
            if (!MatchesSlotKey(slot, slotKey)) {
                continue;
            }

            ushort primaryId = (ushort)item.ModelMain;
            string prefix = (slot.MainHand == 1 || slot.OffHand == 1) ? "w" : "e";
            string modelId = $"{prefix}{primaryId:D4}";

            if (blockedModelIds.Contains(modelId)) {
                continue;
            }

            if (results.Any(r => r.ModelId == modelId)) {
                continue;
            }

            results.Add(new VanillaItem {
                ItemId = item.RowId,
                Name = item.Name.ExtractText(),
                ModelId = modelId,
                IconId = item.Icon,
                ExpansionName = GetExpansionName(item.LevelEquip),
                Origin = DeduceItemOrigin(item)
            });
        }

        return results.OrderBy(i => i.Name).ToList();
    }

    private string DeduceItemOrigin(Item item) {
        // 1. Check if the item can be crafted
        if (this.craftedItemIds.Contains(item.RowId)) {
            return "Artisanat (Crafted)";
        }

        // 2. Heuristics based on tradability and item level for high-end gear
        if (item.IsUntradable) {
            if (item.Rarity == 3) {
                return "Raid / Mémoquartz (Raid/Tomestone)";
            }

            if (item.Rarity == 2) {
                return "Donjon (Dungeon)";
            }

            return "Spécial / Quête (Quest/Reward)";
        }

        return "Achat / Butin standard (Vendor/Drop)";
    }

    private HashSet<string> GetModifiedModelIds(string slotKey, EffectiveCollectionState globalState) {
        var modifiedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var regex = new Regex($@"(e\d{{4}})_{slotKey}|(w\d{{4}})_{slotKey}", RegexOptions.IgnoreCase);

        foreach (var mod in globalState.EffectiveMods.Values.Where(m => m.IsEnabled)) {
            if (!this.scannerManager.ModCache.TryGetValue(mod.Id, out var cache)) {
                continue;
            }

            ExtractModelsFromPaths(cache.ModifiedGamePaths, regex, modifiedIds);
            foreach (var group in cache.OptionGroups.Values) {
                foreach (var optionPaths in group.OptionPaths) {
                    ExtractModelsFromPaths(optionPaths, regex, modifiedIds);
                }
            }
        }
        return modifiedIds;
    }

    private void ExtractModelsFromPaths(IEnumerable<string> paths, Regex regex, HashSet<string> modifiedIds) {
        foreach (var path in paths) {
            var match = regex.Match(path);
            if (match.Success) {
                string modelId = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                modifiedIds.Add(modelId);
            }
        }
    }

    private bool MatchesSlotKey(EquipSlotCategory slot, string slotKey) {
        return slotKey switch {
            "wpn" => slot.MainHand == 1,
            "sub" => slot.OffHand == 1,
            "met" => slot.Head == 1,
            "top" => slot.Body == 1,
            "glv" => slot.Gloves == 1,
            "dwn" => slot.Legs == 1,
            "sho" => slot.Feet == 1,
            "ear" => slot.Ears == 1,
            "nek" => slot.Neck == 1,
            "wrs" => slot.Wrists == 1,
            "rir" => slot.FingerR == 1,
            "ril" => slot.FingerL == 1,
            _ => false
        };
    }

    /// <summary>
    /// Approximates the expansion based on the required equip level.
    /// </summary>
    private string GetExpansionName(byte equipLevel) {
        if (equipLevel <= 50) {
            return "A Realm Reborn";
        }

        if (equipLevel <= 60) {
            return "Heavensward";
        }

        if (equipLevel <= 70) {
            return "Stormblood";
        }

        if (equipLevel <= 80) {
            return "Shadowbringers";
        }

        if (equipLevel <= 90) {
            return "Endwalker";
        }

        return "Dawntrail";
    }
}