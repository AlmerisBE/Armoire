using System.Collections.Generic;

namespace Armoire.Features.Penumbra.Interfaces;

public interface IPenumbraClient {
    /// <summary>
    /// Vérifie si Penumbra est installé, actif, et prêt à recevoir des commandes IPC.
    /// </summary>
    bool IsEnabled();

    /// <summary>
    /// Récupère le nombre total de mods installés dans Penumbra.
    /// </summary>
    int GetModsCount();

    /// <summary>
    /// Récupère les collections actives pour le personnage en ligne.
    /// </summary>
    List<string> GetActiveCollectionHierarchy();
}
