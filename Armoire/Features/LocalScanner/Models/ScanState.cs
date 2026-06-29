namespace Armoire.Features.LocalScanner.Models;

/// <summary>
/// Represents the current state of the mod scanning process.
/// </summary>
public enum ScanState {
    Idle,
    Scanning,
    Paused
}