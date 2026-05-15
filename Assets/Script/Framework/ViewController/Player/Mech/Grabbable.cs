using UnityEngine;
using QFramework;
using QFramework.ViewController.Player;

public class Grabbable : MonoBehaviour, IInteractable
{
    [SerializeField] private string _displayName = "物体";
    public string DisplayName => _displayName;
    public string InteractionText => "移动";

    [SerializeField] private float _followDistance = 1.5f;

    private bool _isGrabbed;
    private Rigidbody2D _rb;
    private Collider2D _collider;
    private Transform _targetGrabPoint;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (!_isGrabbed || _targetGrabPoint == null) return;
        transform.position = _targetGrabPoint.position;
    }

    public void OnInteract(GameObject player)
    {
        _isGrabbed = !_isGrabbed;

        if (_isGrabbed)
        {
            _rb.isKinematic = true;
            _collider.enabled = false;

            _targetGrabPoint = new GameObject("GrabPoint_Temp").transform;
            _targetGrabPoint.SetParent(player.transform.Find("Body") ?? player.transform);
            _targetGrabPoint.localPosition = new Vector3(_followDistance, 0, 0);
        }
        else
        {
            _rb.isKinematic = false;
            _collider.enabled = true;
            _rb.velocity = Vector2.zero;

            if (_targetGrabPoint != null)
                Destroy(_targetGrabPoint.gameObject);
            _targetGrabPoint = null;
        }
    }

    private void OnDestroy()
    {
        if (_targetGrabPoint != null)
            Destroy(_targetGrabPoint.gameObject);
    }
}
