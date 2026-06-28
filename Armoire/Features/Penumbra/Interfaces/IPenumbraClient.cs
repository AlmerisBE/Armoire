using System;
using System.Collections.Generic; // Add this using

public interface IPenumbraClient {
    bool IsEnabled();
    Dictionary<string, string> GetRawModsList();
    int GetModsCount();
    (Guid Id, string Name) GetActiveCollection();
    string GetModDirectory();
}