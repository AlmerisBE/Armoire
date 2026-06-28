namespace Armoire.Features.Penumbra.Interfaces;

using System;

public interface IPenumbraClient {
    bool IsEnabled();
    int GetModsCount();

    // L'API officielle de Penumbra retourne bien un Tuple (Guid, string)
    (Guid Id, string Name) GetActiveCollection();
}