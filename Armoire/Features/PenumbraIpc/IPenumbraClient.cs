namespace Armoire.Features.PenumbraIpc;

using System;
using System.Collections.Generic;

/// <summary>
/// Defines the contract for communicating with the Penumbra IPC API.
/// </summary>
public interface IPenumbraClient {
    // Lifecycle events triggered by Penumbra's IPC
    event Action? OnInitialized;
    event Action? OnDisposed;

    bool IsEnabled();
    Dictionary<string, string> GetRawModsList();
    int GetModsCount();
    (Guid Id, string Name) GetActiveCollection();
    string GetModDirectory();
    bool AddMod(string modDirectory);
    bool ReloadMod(string modDirectory);
    bool EnableMod(string modDirectory);
    void RedrawAll();
}