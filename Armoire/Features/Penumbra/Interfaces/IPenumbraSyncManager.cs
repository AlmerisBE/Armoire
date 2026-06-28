namespace Armoire.Features.Penumbra.Interfaces;

using Armoire.Features.Penumbra.Core.Models;
using System;

public interface IPenumbraSyncManager {
    event Action<PenumbraStatusResult>? OnStatusUpdated;
    void ForceRefresh();
}