using System;
using UnityEngine;
using QFramework.Enum;
using QFramework.ViewController.Player;

public class Interactable : MonoBehaviour, IInteractable
{
    public event Action<GameObject> OnInteracted;
    public event Action OnObjectPlaced;

    public bool IsLocked { get; private set; }

    public void Lock() => IsLocked = true;

    [SerializeField] private string _displayName = "控制台";
    public string DisplayName => _displayName;
    public virtual string InteractionText => "互动";

    [Header("放置目标")]
    [SerializeField] private Transform _placementSocket;
    [SerializeField] private GrabbableType _acceptedType = GrabbableType.None;

    public bool HasSocket => _placementSocket != null;
    public Transform PlacementSocket => _placementSocket;
    public GrabbableType AcceptedType => _acceptedType;

    protected virtual void Awake()
    {
        OnInit();
    }

    protected virtual void OnInit()
    {
    }

    public virtual void OnInteract(GameObject player)
    {
        if (IsLocked) return;
        Debug.Log($"[Interactable] 互动触发: {_displayName}");
        OnInteracted?.Invoke(player);
    }

    public virtual void OnPlaceObject()
    {
        if (IsLocked) return;
        Debug.Log($"[Interactable] 放置物品: {_displayName}");
        OnObjectPlaced?.Invoke();
    }
}
