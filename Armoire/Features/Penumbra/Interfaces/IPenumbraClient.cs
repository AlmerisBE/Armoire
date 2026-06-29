using System;
using System.Collections.Generic;

public interface IPenumbraClient {
    event Action? OnInitialized;
    event Action? OnDisposed;

    bool IsEnabled();
    Dictionary<string, string> GetRawModsList();
    int GetModsCount();
    (Guid Id, string Name) GetActiveCollection();
    string GetModDirectory();
}