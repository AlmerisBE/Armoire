namespace Armoire.Features.ConflictEngine;

using Armoire.Features.ConflictEngine.Models;
using System;

public interface IPenumbraSyncManager {
    event Action<PenumbraStatusResult>? OnStatusUpdated;
    void ForceRefresh();
}