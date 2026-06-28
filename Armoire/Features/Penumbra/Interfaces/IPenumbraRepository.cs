namespace Armoire.Features.Penumbra.Interfaces;

using Armoire.Features.Penumbra.Core.Domain;
using Armoire.Features.Penumbra.Core.Models;
using System.Threading.Tasks;

public interface IPenumbraRepository {
    bool IsLoading { get; }
    Task SyncDataAsync();
    PenumbraCollection? GetCollection(string collectionId);
    EffectiveCollectionState ComputeEffectiveState(string activeCollectionId);
}
