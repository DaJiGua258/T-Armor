using UnityEngine;
using QFramework.Enum;
using QFramework.ViewController.Player;

public class Grabbable : MonoBehaviour, IInteractable
{
    [SerializeField] private string _displayName = "物体";
    public string DisplayName => _displayName;
    public string InteractionText => _isGrabbed ? "放下" : "移动";

    [SerializeField] private float _followDistance = 1.5f;
    public float FollowDistance => _followDistance;

    [SerializeField] private GrabbableType _itemType = GrabbableType.None;
    public GrabbableType ItemType => _itemType;

    public bool IsGrabbed => _isGrabbed;
    public bool IsLocked { get; private set; }

    public void Lock() => IsLocked = true;

    private bool _isGrabbed;
    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void OnInteract(GameObject player)
    {
        if (IsLocked) return;

        Debug.Log($"[Grabbable] OnInteract: {_displayName}, isGrabbed={_isGrabbed}");

        _isGrabbed = !_isGrabbed;

        if (_isGrabbed)
        {
            if (_rb == null)
            {
                Debug.LogError("[Grabbable] 缺少 Rigidbody2D");
                _isGrabbed = false;
                return;
            }
            _rb.isKinematic = true;
        }
        else
        {
            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.velocity = Vector2.zero;
            }
        }
    }

    public void AttachToSocket(Transform socket)
    {
        transform.SetParent(socket);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        _isGrabbed = false;
        IsLocked = true;
        // 保持 kinematic，不受物理影响
    }
}
