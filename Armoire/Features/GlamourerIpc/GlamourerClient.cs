namespace Armoire.Features.GlamourerIpc;

using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using global::Glamourer.Api.Enums;
using global::Glamourer.Api.IpcSubscribers;
using System;
using System.Collections.Generic;

public interface IGlamourerClient {
    bool EquipItem(uint itemId, string slotKey);
}

public class GlamourerClient : IGlamourerClient {
    private readonly IPluginLog pluginLog;
    private readonly SetItem setItemSubscriber;

    public GlamourerClient(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog) {
        this.pluginLog = pluginLog;
        this.setItemSubscriber = new SetItem(pluginInterface);
    }

    public bool EquipItem(uint itemId, string slotKey) {
        // 1. On récupère la valeur typée de l'enum officielle
        ApiEquipSlot slot = GetGlamourerSlot(slotKey);
        if (slot == ApiEquipSlot.Unknown) {
            return false;
        }

        try {
            // 2. Dawntrail : On utilise une List<byte> pour forcer la sérialisation JSON en tableau [0, 0] au lieu de Base64
            IReadOnlyList<byte> stains = new List<byte> { 0, 0 };

            // 3. Appel de la méthode avec la signature parfaite
            // Invoke(int characterIndex, ApiEquipSlot slot, ulong itemId, IReadOnlyList<byte> stains, uint key)
            var result = this.setItemSubscriber.Invoke(0, slot, itemId, stains, 0u);

            return result == GlamourerApiEc.Success;
        } catch (Exception ex) {
            this.pluginLog.Error(ex, "[GlamourerClient] Échec de l'équipement via l'API officielle Glamourer.");
            return false;
        }
    }

    // On retourne l'enum ApiEquipSlot au lieu d'un byte brut
    private ApiEquipSlot GetGlamourerSlot(string slotKey) {
        return slotKey switch {
            "wpn" => ApiEquipSlot.MainHand,
            "sub" => ApiEquipSlot.OffHand,
            "met" => ApiEquipSlot.Head,
            "top" => ApiEquipSlot.Body,
            "glv" => ApiEquipSlot.Hands,
            "dwn" => ApiEquipSlot.Legs,
            "sho" => ApiEquipSlot.Feet,
            "ear" => ApiEquipSlot.Ears,
            "nek" => ApiEquipSlot.Neck,
            "wrs" => ApiEquipSlot.Wrists,
            "rir" => ApiEquipSlot.RFinger,
            "ril" => ApiEquipSlot.LFinger,
            _ => ApiEquipSlot.Unknown
        };
    }
}