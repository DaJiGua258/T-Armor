using System;
using UnityEngine;

namespace QFramework.ViewController.Misc
{
    /// <summary>
    /// 引导状态：挂载在 Emitter 同一节点，跟随鼠标并通知外部（HotbarExecutor）发射进度。
    /// 不直接控制激光，由外部协调者决定。
    /// </summary>
    public class GuidanceState : MonoBehaviour
    {
        [SerializeField] private Emitter _emitter;
        [SerializeField] private bool _extendToLastBulletLand;

        /// <summary> 发射开始（第一颗子弹即将出膛）</summary>
        public event Action OnChannelingStarted;
        /// <summary> 发射结束（最后一颗子弹发射/爆炸）</summary>
        public event Action OnChannelingEnded;

        private enum State { Idle, Channeling }
        private State _currentState;
        private bool _mouseLocked;

        private void Awake()
        {
            if (_emitter == null)
                _emitter = GetComponent<Emitter>();

            if (_emitter == null)
            {
                Debug.LogError("GuidanceState 需要 Emitter 组件", this);
                return;
            }
        }

        private void OnEnable()
        {
            _mouseLocked = false;

            _emitter.OnFireStarted += OnFireStart;
            if (_extendToLastBulletLand)
                _emitter.OnAllBulletsLanded += EndChanneling;
            else
                _emitter.OnFireEnded += EndChanneling;
        }

        private void OnDisable()
        {
            _emitter.OnFireStarted -= OnFireStart;
            _emitter.OnFireEnded -= EndChanneling;
            _emitter.OnAllBulletsLanded -= EndChanneling;
        }

        /// <summary> 由外部（HotbarExecutor）在订阅事件后调用，开始发射 </summary>
        public void Fire()
        {
            _emitter.Fire();
        }

        /// <summary> 锁定鼠标位置，后续子弹固定朝此位置发射 </summary>
        public void LockMouse()
        {
            _mouseLocked = true;
        }

        private void OnFireStart()
        {
            _currentState = State.Channeling;
            OnChannelingStarted?.Invoke();
        }

        private void Update()
        {
            if (_currentState != State.Channeling) return;
            FollowMouse();
        }

        private void FollowMouse()
        {
            if (_mouseLocked) return;

            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;
            transform.position = mousePos;
        }

        private void EndChanneling()
        {
            _currentState = State.Idle;
            OnChannelingEnded?.Invoke();
        }
    }
}
