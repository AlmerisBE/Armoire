namespace Armoire.Features.ModDetails.Presentation;

using Armoire.Features.ModDetails.Models;
using System;

public interface IModDetailsPresenter : IDisposable {
    // --- STATE DATA (What the View reads) ---
    bool IsVisible { get; set; }
    DetailedModState? CurrentState { get; }

    // --- ACTIONS (What the View triggers) ---

    /// <summary>
    /// Opens the details view for a specific mod.
    /// </summary>
    void Open(string modId);

    /// <summary>
    /// Resets the currently displayed mod to its default settings.
    /// </summary>
    void ResetCurrentMod();

    /// <summary>
    /// Equips a specific item in-game via Glamourer.
    /// </summary>
    void EquipItem(uint itemId, string slotKey);

    /// <summary>
    /// Opens the search window to find a vanilla replacement for the specified slot.
    /// </summary>
    void OpenVanillaReplacement(string slotKey, string originalProviderId);

    /// <summary>
    /// Retrieves the active texture provider selection, falling back to the original if not modified.
    /// </summary>
    string GetSelectedProvider(string slotKey, string originalProviderId);

    /// <summary>
    /// Stores the user's texture provider selection in the current session.
    /// </summary>
    void SetSelectedProvider(string slotKey, string providerId);
}