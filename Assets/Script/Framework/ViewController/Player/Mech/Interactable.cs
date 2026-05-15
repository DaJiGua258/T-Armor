using UnityEngine;
using UnityEngine.Events;
using QFramework.ViewController.Player;

public class Interactable : MonoBehaviour, IInteractable
{
    [SerializeField] private string _displayName = "控制台";
    public string DisplayName => _displayName;
    public string InteractionText => "互动";

    [SerializeField] private UnityEvent OnInteractEvent;

    public void OnInteract(GameObject player)
    {
        OnInteractEvent?.Invoke();
    }
}
