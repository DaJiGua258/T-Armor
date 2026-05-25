using QFramework.Command;
using QFramework.Enum;
using QFramework.Manager;
using QFramework.System;
using QFramework.Utility;
using UnityEngine;

namespace QFramework.ViewController.Item
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class DropItemPickup : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        [Header("磁吸参数")]
        [SerializeField] private float _attractRadius = 3f;
        [SerializeField] private float _acceleration = 15f;
        [SerializeField] private float _maxSpeed = 10f;

        [Header("旋转参数")]
        [SerializeField] private float _rotationSpeed = 10f;

        [Header("拾取VFX")]
        [SerializeField] private GameObject _pickUpVFX;
        [SerializeField] private float _vfxDuration = 2f;

        private Transform _player;
        private bool _attracting;
        private Vector2 _velocity;
        private IInvenotrySystem _inventory;
        private IObjectPoolUtility _objectPool;
        private ITimerUtility _timer;
        private Transform _mesh;
        private Transform _shadow;
        private CircleCollider2D _trigger;

        private void Awake()
        {
            _inventory = this.GetSystem<IInvenotrySystem>();
            _objectPool = this.GetUtility<IObjectPoolUtility>();
            _timer = this.GetUtility<ITimerUtility>();
            _mesh = transform.Find("Mesh");
            _shadow = transform.Find("Shadow");

            // Trigger 已预制在 prefab 上，仅保底创建
            _trigger = GetComponent<CircleCollider2D>();
            if (_trigger == null)
            {
                _trigger = gameObject.AddComponent<CircleCollider2D>();
                _trigger.isTrigger = true;
            }
            _trigger.radius = _attractRadius;
        }

        private void OnEnable()
        {
            _attracting = false;
            _player = null;
            _velocity = Vector2.zero;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _player = other.transform;
            _attracting = true;
        }

        private void Update()
        {
            // 始终旋转 Mesh 和 Shadow
            float angle = Time.time * _rotationSpeed;
            if (_mesh != null)
                _mesh.rotation = Quaternion.Euler(0, 0, angle);
            if (_shadow != null)
                _shadow.rotation = Quaternion.Euler(0, 0, angle);

            if (!_attracting) return;
            if (!_inventory.HasFreeSlot()) return;

            // 加速度磁吸
            Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            _velocity += dir * _acceleration * Time.deltaTime;
            _velocity = Vector2.ClampMagnitude(_velocity, _maxSpeed);
            transform.position += (Vector3)_velocity * Time.deltaTime;

            // 背对玩家时减速
            if (Vector2.Dot(dir, _velocity.normalized) < 0f)
                _velocity *= 0.5f;

            if (Vector2.Distance(transform.position, _player.position) < 0.3f)
                TryPickup();
        }

        private void TryPickup()
        {
            var itemType = DropItemManager.Instance.SettleDrop();
            if (itemType == ItemTypeEnum.None) return;

            this.SendCommand(new PickUpCommand.AddDropItem(itemType));

            if (_pickUpVFX != null)
            {
                var vfx = _objectPool.GetObject(_pickUpVFX, transform.position, Quaternion.identity);
                var captured = vfx;
                _timer.AddOnce(() => _objectPool.PushObject(captured), _vfxDuration);
            }

            _objectPool.PushObject(gameObject);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _attracting = false;
                _player = null;
            }
        }
    }
}
