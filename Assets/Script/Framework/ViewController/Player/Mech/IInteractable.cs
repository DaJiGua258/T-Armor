using UnityEngine;

namespace QFramework.ViewController.Player
{
    public interface IInteractable
    {
        string DisplayName { get; }
        string InteractionText { get; }
        void OnInteract(GameObject player);
    }
}
