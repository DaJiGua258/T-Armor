using QFramework.Event;
using QFramework.Utility;
using QFramework.ViewController.UI;
using UnityEngine;

namespace QFramework.ViewController.Player
{
    [RequireComponent(typeof(LineRenderer))]
    public class GuidanceLaser : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        [SerializeField] private Transform _body;
        [SerializeField] private Transform _originTransform;
        [SerializeField] private float _maxDistance = 50f;
        [SerializeField] private float _cooldownTime = 0.5f;

        [Header("颜色设置")]
        [SerializeField] private Color _interactionColor = Color.red;
        [SerializeField] private Color _channelingColor = Color.green;

        private LineRenderer _line;
        private bool _isActive;
        private bool _isChanneling;
        private bool _isInCombat;
        private float _cooldownTimer;

        /// <summary> 引导期间追踪的鼠标位置，供物品效果使用 </summary>
        public Vector3 ChannelingMousePos { get; private set; }

        void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.enabled = false;
        }

        void Start()
        {
            TypeEventSystem.Global.Register<PlayerEvent.GuidanceLaserShow>(
                e => OnShow()
            ).UnRegisterWhenGameObjectDestroyed(gameObject);

            TypeEventSystem.Global.Register<PlayerEvent.GuidanceLaserHide>(
                e => OnHide()
            ).UnRegisterWhenGameObjectDestroyed(gameObject);

            TypeEventSystem.Global.Register<PlayerEvent.GuidanceLaserSetChanneling>(
                e => OnChannelingStart()
            ).UnRegisterWhenGameObjectDestroyed(gameObject);

            TypeEventSystem.Global.Register<PlayerEvent.SwitchAimingMode>(
                e => OnAimingModeChanged(e.Mode)
            ).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        void Update()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
                if (_cooldownTimer <= 0f)
                {
                    _cooldownTimer = 0f;
                    if (!_isInCombat && _isActive)
                        ShowRedLaser();
                }
            }

            if (_isChanneling)
                ChannelingMousePos = this.GetUtility<IInputUtility>().GetMousePos();
        }

        void LateUpdate()
        {
            if (!_line.enabled) return;

            Vector3 origin = (_originTransform ? _originTransform : transform).position;
            origin.z = 0f;

            Transform dirTransform = _body ? _body : transform;
            Vector3 dir = dirTransform.right;

            Vector3 mouseWorld = this.GetUtility<IInputUtility>().GetMousePos();
            mouseWorld.z = 0f;
            float mouseDist = Vector3.Distance(origin, mouseWorld);
            float laserLength = Mathf.Min(mouseDist, _maxDistance);

            Vector3 end = origin + dir * laserLength;

            _line.SetPosition(0, origin);
            _line.SetPosition(1, end);
        }

        private void OnShow()
        {
            _isActive = true;
            _cooldownTimer = 0f;
            if (!_isInCombat)
                ShowRedLaser();
        }

        private void OnHide()
        {
            bool wasChanneling = _isChanneling;
            _isChanneling = false;

            if (!wasChanneling)
                _isActive = false;

            HideLaser();

            if (wasChanneling && !_isInCombat)
                _cooldownTimer = _cooldownTime;
        }

        private void OnChannelingStart()
        {
            if (_isInCombat) return;
            _isChanneling = true;
            _isActive = true;
            _cooldownTimer = 0f;
            ChannelingMousePos = this.GetUtility<IInputUtility>().GetMousePos();
            SetLaserColor(_channelingColor);
            _line.enabled = true;
        }

        private void OnAimingModeChanged(AimingModeEnum mode)
        {
            _isInCombat = mode == AimingModeEnum.Combat;

            if (_isInCombat)
            {
                _isChanneling = false;
                _cooldownTimer = 0f;
                HideLaser();
            }
            else if (_isActive && !_isChanneling)
            {
                ShowRedLaser();
            }
        }

        private void ShowRedLaser()
        {
            SetLaserColor(_interactionColor);
            _line.enabled = true;
        }

        private void SetLaserColor(Color color)
        {
            _line.startColor = color;
            _line.endColor = color;
        }

        private void HideLaser()
        {
            _line.enabled = false;
        }
    }
}
