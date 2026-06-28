namespace Armoire.Features.Penumbra.Interfaces;

using System;

public interface IPenumbraClient {
    bool IsEnabled();
    int GetModsCount();
    (Guid Id, string Name) GetActiveCollection();
    string GetModDirectory();
}