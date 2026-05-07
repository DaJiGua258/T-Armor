using QFramework.Event;
using UnityEngine;

namespace QFramework.ViewController.Misc
{
    /// <summary>
    /// 引导状态：挂载在 Emitter 同一节点，自动监测发射进度并控制 GuidanceLaser。
    /// 内部维护静态实例，保证同时只有一个引导活跃。
    /// </summary>
    public class GuidanceState : MonoBehaviour
    {
        private static GuidanceState _activeInstance;

        [SerializeField] private Emitter _emitter;
        [SerializeField] private bool _extendToLastBulletLand;

        private enum State { Idle, Channeling }
        private State _currentState;
        private float _stateEnterTime;
        private float _channelingDuration;
        private bool _waitingForLanding;

        private void Awake()
        {
            if (_emitter == null)
                _emitter = GetComponent<Emitter>();

            if (_emitter == null)
            {
                Debug.LogError("GuidanceState 需要 Emitter 组件", this);
                return;
            }

            // Mode A: 按发射器参数计算
            float duration = _emitter.TotalChannelingTime;

            // Mode B: 加最后一发子弹的飞行时间
            if (_extendToLastBulletLand)
            {
                float maxDistance = _emitter.SpawnOffset.magnitude + _emitter.SpreadRadius;
                float flightTime = maxDistance / _emitter.Speed;
                duration += flightTime;
            }

            _channelingDuration = duration;
        }

        private void OnEnable()
        {
            if (_activeInstance != null && _activeInstance != this)
                Destroy(_activeInstance.gameObject);

            _activeInstance = this;

            _emitter.OnFireStarted += OnFireStart;
            _emitter.OnFireEnded += OnFireEnd;

            // 自动开火
            _emitter.Fire();
        }

        private void OnDisable()
        {
            if (_activeInstance == this)
                _activeInstance = null;

            _emitter.OnFireStarted -= OnFireStart;
            _emitter.OnFireEnded -= OnFireEnd;
        }

        private void OnFireStart()
        {
            _currentState = State.Channeling;
            _stateEnterTime = Time.time;
            _waitingForLanding = false;
            TypeEventSystem.Global.Send(new PlayerEvent.GuidanceLaserSetChanneling());
        }

        private void OnFireEnd()
        {
            if (_extendToLastBulletLand)
                _waitingForLanding = true;
            else
                EndChanneling();
        }

        private void Update()
        {
            if (_currentState != State.Channeling) return;

            // 引导期间跟随鼠标
            FollowMouse();

            // Mode B: 等待飞行时间结束
            if (_waitingForLanding && Time.time - _stateEnterTime >= _channelingDuration)
                EndChanneling();
        }

        private void FollowMouse()
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;
            transform.position = mousePos;
        }

        private void EndChanneling()
        {
            _currentState = State.Idle;
            _waitingForLanding = false;
            TypeEventSystem.Global.Send(new PlayerEvent.GuidanceLaserHide());
        }
    }
}
