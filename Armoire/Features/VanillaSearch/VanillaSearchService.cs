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

    public VanillaSearchService(IDataManager dataManager, IModScannerManager scannerManager) {
        this.dataManager = dataManager;
        this.scannerManager = scannerManager;
    }

    public List<VanillaItem> GetAvailableReplacements(string slotKey, EffectiveCollectionState globalState) {
        var results = new List<VanillaItem>();
        var itemSheet = this.dataManager.GetExcelSheet<Item>();
        if (itemSheet == null) {
            return results;
        }

        // 1. Identify which models are already altered by other active mods
        var blockedModelIds = GetModifiedModelIds(slotKey, globalState);

        // 2. Scan Lumina database for matching items
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

            // 3. Exclude if another active mod is already overriding this model
            if (blockedModelIds.Contains(modelId)) {
                continue;
            }

            // Prevent UI duplicates (many leveling items share the exact same 3D model)
            if (results.Any(r => r.ModelId == modelId)) {
                continue;
            }

            results.Add(new VanillaItem {
                ItemId = item.RowId,
                Name = item.Name.ExtractText(),
                ModelId = modelId,
                IconId = item.Icon
            });
        }

        // Sort alphabetically for easier navigation
        return results.OrderBy(i => i.Name).ToList();
    }

    /// <summary>
    /// Scans all active mods in the collection to extract the model IDs they are modifying for a given slot.
    /// </summary>
    private HashSet<string> GetModifiedModelIds(string slotKey, EffectiveCollectionState globalState) {
        var modifiedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Regex to capture "e0123" or "w0123" if it's followed by our target slot (e.g., "_top")
        var regex = new Regex($@"(e\d{{4}})_{slotKey}|(w\d{{4}})_{slotKey}", RegexOptions.IgnoreCase);

        foreach (var mod in globalState.EffectiveMods.Values.Where(m => m.IsEnabled)) {
            if (!this.scannerManager.ModCache.TryGetValue(mod.Id, out var cache)) {
                continue;
            }

            // Scan default paths
            ExtractModelsFromPaths(cache.ModifiedGamePaths, regex, modifiedIds);

            // Scan options paths (to be safe, we block models touched by ANY option, even inactive ones)
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
                // Group 1 is 'eXXXX', Group 2 is 'wXXXX'
                string modelId = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                modifiedIds.Add(modelId);
            }
        }
    }

    /// <summary>
    /// Maps our internal short string key to the Lumina EquipSlotCategory struct.
    /// </summary>
    private bool MatchesSlotKey(EquipSlotCategory slot, string slotKey) {
        return slotKey switch {
            "wpn" => slot.MainHand == 1 || slot.OffHand == 1,
            "met" => slot.Head == 1,
            "top" => slot.Body == 1,
            "glv" => slot.Gloves == 1,
            "dwn" => slot.Legs == 1,
            "sho" => slot.Feet == 1,
            "ear" => slot.Ears == 1,
            "nek" => slot.Neck == 1,
            "wrs" => slot.Wrists == 1,
            "rir" => slot.FingerR == 1 || slot.FingerL == 1,
            _ => false
        };
    }
}