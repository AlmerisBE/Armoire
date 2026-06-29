namespace Armoire.Features.ConflictEngine;

using Armoire.Features.ConflictEngine.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IPenumbraRepository {
    bool IsLoading { get; }
    bool IsStale { get; }
    void MarkStale();

    Task SyncDataAsync();
    PenumbraCollection? GetCollection(string collectionId);
    IEnumerable<PenumbraCollection> GetAllCollections();
    EffectiveCollectionState ComputeEffectiveState(string activeCollectionId);
}