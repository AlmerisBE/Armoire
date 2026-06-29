namespace Armoire.Features.ConflictEngine;

using Armoire.Features.ConflictEngine.Models;

public interface IPenumbraAnalyzer {
    PenumbraStatusResult GetStatusReport();
}