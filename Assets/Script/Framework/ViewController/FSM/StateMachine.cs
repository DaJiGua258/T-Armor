using System;
using System.Collections.Generic;
using UnityEngine;

namespace QFramework.ViewController.FSM
{
    public class StateMachine<TOwner>
    {
        private readonly Dictionary<Type, IState> _states = new Dictionary<Type, IState>();
        private IState _currentState;
        private Type _currentStateType;

        // 当前激活的状态对象
        public IState CurrentState => _currentState;

        // 当前激活状态的类型
        public Type CurrentStateType => _currentStateType;

        // ── 注册 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 注册一个状态实例，支持链式调用。
        /// 若同类型状态已注册，将覆盖原有实例。
        /// </summary>
        public void AddState<TState>(TState state) where TState : IState
        {
            _states[typeof(TState)] = state;
        }

        // ── 启动 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 启动状态机并进入指定的初始状态（跳过 OnCondition 检查）。
        /// 应在所有 AddState 调用完成后调用一次。
        /// </summary>
        public void StartState<TState>() where TState : IState
        {
            var t = typeof(TState); // 获取状态类型

            // 如果状态未注册，则抛出异常
            if (!_states.TryGetValue(t, out var initial))
            {
                throw new InvalidOperationException(
                    $"[FSM] 状态 {t.Name} 尚未注册，请先调用 AddState。");
            }

            // 设置当前状态
            _currentStateType = t;
            _currentState = initial; // 设置当前状态
            _currentState.OnEnter(); // 调用状态的进入方法
        }

        // ── 切换 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 切换到目标状态。
        /// 若目标状态未注册、已是当前状态、或 OnCondition 返回 false，则忽略本次请求。
        /// </summary>
        public void ChangeState<TState>() where TState : IState
        {
            var t = typeof(TState);

            if (_currentStateType == t) return;

            if (!_states.TryGetValue(t, out var next)) 
            {
                Debug.LogError($"[FSM] 状态 {t.Name} 尚未注册，请先调用 AddState。");
                return;
            }

            if (!next.OnCondition()) return;

            _currentState?.OnExit();
            _currentStateType = t;
            _currentState = next;
            _currentState.OnEnter();
        }

        // ── 驱动 ──────────────────────────────────────────────────────────────

        /// <summary>在宿主 MonoBehaviour.Update 中调用以驱动当前状态。</summary>
        public void Update() => _currentState?.OnUpdate();

        /// <summary>在宿主 MonoBehaviour.FixedUpdate 中调用以驱动当前状态。</summary>
        public void FixedUpdate() => _currentState?.OnFixedUpdate();

        // ── 查询 ──────────────────────────────────────────────────────────────

        /// <summary>判断当前状态是否为指定类型。</summary>
        public bool IsCurrentState<TState>() where TState : IState
            => _currentStateType == typeof(TState);
    }
}
