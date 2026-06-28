namespace Armoire.Features.Penumbra.Core.Models;

/// <summary>
/// Represents the current state of the mod scanning process.
/// </summary>
public enum ScanState {
    Idle,
    Scanning,
    Paused
}