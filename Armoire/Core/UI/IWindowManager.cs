using System;

namespace Armoire.Core.UI;

/// <summary>
/// Gère le cycle de vie et l'affichage des fenêtres de l'interface utilisateur.
/// </summary>
public interface IWindowManager : IDisposable
{
    /// <summary>
    /// Alterne l'état d'affichage de la fenêtre principale.
    /// </summary>
    void ToggleMainWindow();

    /// <summary>
    /// Alterne l'état d'affichage de la fenêtre de configuration.
    /// </summary>
    void ToggleConfigWindow();

    /// <summary>
    /// Appelle la boucle de rendu pour toutes les fenêtres gérées.
    /// </summary>
    void Draw();
}
