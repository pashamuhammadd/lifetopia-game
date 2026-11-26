using System;

namespace Lifetopia.Interaction
{
    /// <summary>
    /// Optional for an <see cref="IInteractable"/> whose prompt text changes while the player stays
    /// next to it (e.g. a farm plot: "Till soil" → "Plant seeds"). The prompt UI listens to the
    /// current target only, so nothing is polled every frame.
    /// </summary>
    public interface IPromptChangeNotifier
    {
        event Action PromptChanged;
    }
}
