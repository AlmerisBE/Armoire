namespace Armoire.Features.Penumbra.Interfaces;

using Armoire.Features.Penumbra.Core.Models;

public interface IPenumbraAnalyzer {
    PenumbraStatusResult GetStatusReport();
}