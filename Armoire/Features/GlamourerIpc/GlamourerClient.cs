namespace Armoire.Features.GlamourerIpc;

using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using global::Glamourer.Api.Enums;
using global::Glamourer.Api.IpcSubscribers;
using System;
using System.Collections.Generic;

public interface IGlamourerClient {
    bool EquipItem(uint itemId, string slotKey);

    /// <summary>
    /// Requests the current appearance and equipment state of the player character encoded as a Base64 string.
    /// </summary>
    string GetCharacterStateBase64();

    /// <summary>
    /// Applies a previously saved Base64 state to the local player character.
    /// </summary>
    bool ApplyStateBase64(string base64State);

    /// <summary>
    /// Requests the current appearance and equipment state of the player character encoded as a JSON string.
    /// </summary>
    string GetCharacterStateJson();
}

public class GlamourerClient : IGlamourerClient {
    private readonly IPluginLog pluginLog;
    private readonly SetItem setItemSubscriber;
    private readonly GetStateBase64 getStateBase64Subscriber;
    private readonly ApplyState applyStateSubscriber;
    private readonly GetState getStateSubscriber;

    public GlamourerClient(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog) {
        this.pluginLog = pluginLog;
        this.setItemSubscriber = new SetItem(pluginInterface);
        this.getStateBase64Subscriber = new GetStateBase64(pluginInterface);
        this.applyStateSubscriber = new ApplyState(pluginInterface);
        this.getStateSubscriber = new GetState(pluginInterface);
    }

    public bool EquipItem(uint itemId, string slotKey) {
        ApiEquipSlot slot = GetGlamourerSlot(slotKey);
        if (slot == ApiEquipSlot.Unknown) {
            return false;
        }

        try {
            IReadOnlyList<byte> stains = new List<byte> { 0, 0 };
            var result = this.setItemSubscriber.Invoke(0, slot, itemId, stains, 0u);
            return result == GlamourerApiEc.Success;
        } catch (Exception ex) {
            this.pluginLog.Error(ex, "[GlamourerClient] Failed to equip item via Glamourer IPC.");
            return false;
        }
    }

    public string GetCharacterStateBase64() {
        try {
            // Index 0 generally targets the local player character
            var (errorCode, base64String) = this.getStateBase64Subscriber.Invoke(0);

            if (errorCode == GlamourerApiEc.Success && !string.IsNullOrEmpty(base64String)) {
                return base64String;
            }

            this.pluginLog.Warning($"[GlamourerClient] Failed to fetch Base64 state. Error: {errorCode}");
            return string.Empty;
        } catch (Exception ex) {
            this.pluginLog.Error(ex, "[GlamourerClient] Exception while invoking GetStateBase64.");
            return string.Empty;
        }
    }

    public string GetCharacterStateJson() {
        try {
            // Glamourer returns a tuple of (GlamourerApiEc, Newtonsoft.Json.Linq.JObject)
            var (errorCode, jsonObject) = this.getStateSubscriber.Invoke(0);

            if (errorCode == GlamourerApiEc.Success && jsonObject != null) {
                // Convert the JObject into a clean, unformatted JSON string
                return jsonObject.ToString(Newtonsoft.Json.Formatting.None);
            }

            this.pluginLog.Warning($"[GlamourerClient] Failed to fetch JSON state. Error: {errorCode}");
            return string.Empty;
        } catch (Exception ex) {
            this.pluginLog.Error(ex, "[GlamourerClient] Exception while invoking GetState.");
            return string.Empty;
        }
    }

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

    public bool ApplyStateBase64(string base64State) {
        if (string.IsNullOrEmpty(base64State)) {
            return false;
        }

        try {
            // Apply the state to the local player character (index 0)
            this.applyStateSubscriber.Invoke(base64State, 0);
            return true;
        } catch (Exception ex) {
            this.pluginLog.Error(ex, "[GlamourerClient] Failed to apply Base64 state via Glamourer IPC.");
            return false;
        }
    }
}