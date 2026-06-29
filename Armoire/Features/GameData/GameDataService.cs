namespace Armoire.Features.GameData;

using Armoire.Core.Localization;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class GameDataService : IGameDataService {
    private readonly IDataManager dataManager;
    private readonly IPluginLog pluginLog;
    private readonly ILocalizationService loc;

    private readonly Dictionary<string, (string Name, uint IconId)> equipmentModelCache = new(StringComparer.OrdinalIgnoreCase);

    private readonly Regex equipmentPathRegex = new Regex(@"chara/equipment/(e\d{4})/", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly Regex weaponPathRegex = new Regex(@"chara/weapon/(w\d{4})/", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public GameDataService(IDataManager dataManager, IPluginLog pluginLog, ILocalizationService loc) {
        this.dataManager = dataManager;
        this.pluginLog = pluginLog;
        this.loc = loc;
        BuildItemCache();
    }

    private void BuildItemCache() {
        var itemSheet = this.dataManager.GetExcelSheet<Item>();
        if (itemSheet == null) {
            return;
        }

        foreach (var item in itemSheet) {
            if (item.ModelMain == 0) {
                continue;
            }

            string itemName = item.Name.ExtractText();
            if (string.IsNullOrEmpty(itemName)) {
                continue;
            }

            ushort primaryId = (ushort)item.ModelMain;
            var slot = item.EquipSlotCategory.Value;

            string modelId = string.Empty;
            string slotKey = string.Empty;

            if (slot.MainHand == 1 || slot.OffHand == 1) { modelId = $"w{primaryId:D4}"; slotKey = "wpn"; } else if (slot.Head == 1) { modelId = $"e{primaryId:D4}"; slotKey = "met"; } else if (slot.Body == 1) { modelId = $"e{primaryId:D4}"; slotKey = "top"; } else if (slot.Gloves == 1) { modelId = $"e{primaryId:D4}"; slotKey = "glv"; } else if (slot.Legs == 1) { modelId = $"e{primaryId:D4}"; slotKey = "dwn"; } else if (slot.Feet == 1) { modelId = $"e{primaryId:D4}"; slotKey = "sho"; } else if (slot.Ears == 1) { modelId = $"e{primaryId:D4}"; slotKey = "ear"; } else if (slot.Neck == 1) { modelId = $"e{primaryId:D4}"; slotKey = "nek"; } else if (slot.Wrists == 1) { modelId = $"e{primaryId:D4}"; slotKey = "wrs"; } else if (slot.FingerR == 1 || slot.FingerL == 1) { modelId = $"e{primaryId:D4}"; slotKey = "rir"; }

            if (!string.IsNullOrEmpty(modelId) && !string.IsNullOrEmpty(slotKey)) {
                this.equipmentModelCache.TryAdd($"{modelId}_{slotKey}", (itemName, item.Icon));
            }
        }
    }

    public ResolvedItem ResolveItem(string gamePath) {
        var result = new ResolvedItem { Name = this.loc.GetString("GameData_UnknownPath"), SlotKey = "unknown", IconId = 0 };
        if (string.IsNullOrWhiteSpace(gamePath)) {
            return result;
        }

        string lowerPath = gamePath.ToLowerInvariant();
        result.SlotKey = ExtractSlotKey(lowerPath);

        // 1. Gear
        var equipMatch = this.equipmentPathRegex.Match(gamePath);
        if (equipMatch.Success) {
            string modelId = equipMatch.Groups[1].Value;
            if (!string.IsNullOrEmpty(result.SlotKey) && this.equipmentModelCache.TryGetValue($"{modelId}_{result.SlotKey}", out var cacheData)) {
                result.Name = AppendFileType(cacheData.Name, lowerPath);
                result.IconId = cacheData.IconId;
                return result;
            }
            result.Name = AppendFileType(string.Format(this.loc.GetString("GameData_GenericEquip"), modelId), lowerPath);
            return result;
        }

        // 2. Weapons
        var weaponMatch = this.weaponPathRegex.Match(gamePath);
        if (weaponMatch.Success) {
            string modelId = weaponMatch.Groups[1].Value;
            if (this.equipmentModelCache.TryGetValue($"{modelId}_wpn", out var cacheData)) {
                result.Name = AppendFileType(cacheData.Name, lowerPath);
                result.IconId = cacheData.IconId;
                return result;
            }
            result.Name = AppendFileType(string.Format(this.loc.GetString("GameData_GenericWeapon"), modelId), lowerPath);
            return result;
        }

        // 3. Customisation
        if (lowerPath.Contains("chara/human/")) {
            result.SlotKey = "custom";
            if (lowerPath.Contains("face")) {
                result.Name = AppendFileType(this.loc.GetString("GameData_CustomFace"), lowerPath);
            } else if (lowerPath.Contains("hair")) {
                result.Name = AppendFileType(this.loc.GetString("GameData_CustomHair"), lowerPath);
            } else if (lowerPath.Contains("tail")) {
                result.Name = AppendFileType(this.loc.GetString("GameData_CustomTail"), lowerPath);
            } else {
                result.Name = AppendFileType(this.loc.GetString("GameData_CustomBody"), lowerPath);
            }

            return result;
        }

        result.Name = AppendFileType(this.loc.GetString("GameData_SystemUi"), lowerPath);
        return result;
    }

    private string ExtractSlotKey(string path) {
        if (path.Contains("_met")) {
            return "met";
        }

        if (path.Contains("_top")) {
            return "top";
        }

        if (path.Contains("_glv")) {
            return "glv";
        }

        if (path.Contains("_dwn")) {
            return "dwn";
        }

        if (path.Contains("_sho")) {
            return "sho";
        }

        if (path.Contains("_ear")) {
            return "ear";
        }

        if (path.Contains("_nek")) {
            return "nek";
        }

        if (path.Contains("_wrs")) {
            return "wrs";
        }

        if (path.Contains("_rir") || path.Contains("_ril")) {
            return "rir";
        }

        if (path.Contains("weapon")) {
            return "wpn";
        }

        return "unknown";
    }

    private string AppendFileType(string name, string path) {
        if (path.EndsWith(".mdl")) {
            return $"{name}{this.loc.GetString("GameData_SuffixMdl")}";
        }

        if (path.EndsWith(".mtrl")) {
            return $"{name}{this.loc.GetString("GameData_SuffixMtrl")}";
        }

        if (path.EndsWith(".tex")) {
            return $"{name}{this.loc.GetString("GameData_SuffixTex")}";
        }

        if (path.EndsWith(".pap")) {
            return $"{name}{this.loc.GetString("GameData_SuffixPap")}";
        }

        return name;
    }
}