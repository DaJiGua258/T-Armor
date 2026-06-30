using System;
using System.Collections.Generic;
using QFramework;
using QFramework.UtilityKit;
using Unity.VisualScripting;
using UnityEngine;

namespace QFramework.Utility
{
    // ── 计时器状态 ───────────────────────────────────────────
    public enum TimerState
    {
        NotStarted,
        Running,
        Finished
    }

    // ── 计时器数据（内部使用）───────────────────────────────
    public class TimerEvent
    {
        // 配置
        public float  DelayTime;
        public float  durationTime;
        public int    RepeatCount;
        public float  IntervalTime;

        // 回调
        public Action Action;
        public Action CallBack;

        // 运行时
        public TimerState State;
        public float      ElapsedTime;
        public float      PhaseStartTime;
        public int        ExecutedCount;
        public bool       CanExecute;

        // 阶段标志
        public bool DelayFinished;
        public bool ContinueFinished;
        public bool IntervalFinished;
        public bool CallBackInvoked;

        internal static void MarkFinished(TimerEvent evt)
        {
            evt.State = TimerState.Finished;
            if (evt.CallBackInvoked) return;
            evt.CallBackInvoked = true;
            evt.CallBack?.Invoke();
        }

        internal void Reset()
        {
            DelayTime        = 0f;
            durationTime     = 0f;
            RepeatCount      = 1;
            IntervalTime     = 0f;
            Action           = null;
            CallBack         = null;
            State            = TimerState.NotStarted;
            ElapsedTime      = 0f;
            PhaseStartTime   = 0f;
            ExecutedCount    = 0;
            CanExecute       = true;
            DelayFinished    = false;
            ContinueFinished = false;
            IntervalFinished = false;
            CallBackInvoked  = false;
        }
    }

    // ── 对象池 ───────────────────────────────────────────────
    public class TimerEventPool
    {
        private readonly Queue<TimerEvent> _pool = new();
        private readonly int               _maxSize;

        public TimerEventPool(int maxSize = 64)
        {
            _maxSize = maxSize;
        }

        public TimerEvent Get()
        {
            var evt = _pool.Count > 0 ? _pool.Dequeue() : new TimerEvent();
            evt.Reset();
            return evt;
        }

        public void Return(TimerEvent evt)
        {
            if (_pool.Count >= _maxSize) return;
            evt.Reset();
            _pool.Enqueue(evt);
        }
    }

    // ── 接口 ────────────────────────────────────────────────
    public interface ITimerUtility : IUtility
    {
        /// <summary>
        /// 基础接口：完整参数，Finished 后自动回收。
        /// </summary>
        void AddTimerEvent(
            float  delayTime,
            float  durationTime,
            int    repeatCount,
            float  intervalTime,
            Action action,
            Action callBack = null);

        /// <summary>
        /// 单次持续事件：延迟后持续执行 durationTime 秒，结束后触发 callBack。
        /// </summary>
        void AddOnceWithDuration(
            float  durationTime,
            Action action,
            float  delayTime = 0f,
            Action callBack  = null);

        /// <summary>
        /// 单次非持续事件：延迟后触发一帧，结束后触发 callBack。
        /// </summary>
        void AddOnce(
            Action action,
            float  delayTime = 0f,
            Action callBack  = null);

        /// <summary>
        /// 重复持续事件：延迟后持续执行 durationTime 秒，间隔 intervalTime 后重复，共执行 repeatCount 次。
        /// </summary>
        void AddRepeatWithDuration(
            float  durationTime,
            float  intervalTime,
            int    repeatCount,
            Action action,
            float  delayTime = 0f,
            Action callBack  = null);

        /// <summary>
        /// 重复非持续事件：延迟后触发一帧，间隔 intervalTime 后重复，共执行 repeatCount 次。
        /// </summary>
        void AddRepeat(
            float  intervalTime,
            int    repeatCount,
            Action action,
            float  delayTime = 0f,
            Action callBack  = null);

        /// <summary>
        /// 无限循环非持续事件：每隔 intervalTime 触发一帧。
        /// </summary>
        void AddLoop(
            float  intervalTime,
            Action action,
            float  delayTime = 0f);

        /// <summary>
        /// 无限循环持续事件：持续执行 durationTime 秒，间隔 intervalTime 后再次执行。
        /// </summary>
        void AddLoopWithDuration(
            float  durationTime,
            float  intervalTime,
            Action action,
            float  delayTime = 0f);
    }

    // ── 实现 ────────────────────────────────────────────────
    public class TimerUtility : ITimerUtility
    {
        private readonly List<TimerEvent> _events = new();
        private readonly TimerEventPool   _pool   = new();

        public TimerUtility()
        {
            if (Application.isPlaying)
            {
                var go = new GameObject(nameof(TimerRunner));
                GameObject.DontDestroyOnLoad(go);
                go.AddComponent<TimerRunner>()._timerEvent += Tick;
            }
        }

        // ── 基础接口 ────────────────────────────────────────

        public void AddTimerEvent(
            float  delayTime,
            float  durationTime,
            int    repeatCount,
            float  intervalTime,
            Action action,
            Action callBack = null)
        {
            var evt = _pool.Get();
            InitEvent(evt, delayTime, durationTime, repeatCount, intervalTime, action, callBack);
            _events.Add(evt);
        }

        // ── 语义封装 ────────────────────────────────────────

        public void AddOnceWithDuration(
            float  durationTime,
            Action action,
            float  delayTime = 0f,
            Action callBack  = null)
        {
            AddTimerEvent(
                delayTime:    delayTime,
                durationTime: durationTime,
                repeatCount:  1,
                intervalTime: 0f,
                action:       action,
                callBack:     callBack);
        }

        public void AddOnce(
            Action action,
            float  delayTime = 0f,
            Action callBack  = null)
        {
            AddTimerEvent(
                delayTime:    delayTime,
                durationTime: 0f,
                repeatCount:  1,
                intervalTime: 0f,
                action:       action,
                callBack:     callBack);
        }

        public void AddRepeatWithDuration(
            float  durationTime,
            float  intervalTime,
            int    repeatCount,
            Action action,
            float  delayTime = 0f,
            Action callBack  = null)
        {
            AddTimerEvent(
                delayTime:    delayTime,
                durationTime: durationTime,
                repeatCount:  repeatCount,
                intervalTime: intervalTime,
                action:       action,
                callBack:     callBack);
        }

        public void AddRepeat(
            float  intervalTime,
            int    repeatCount,
            Action action,
            float  delayTime = 0f,
            Action callBack  = null)
        {
            AddTimerEvent(
                delayTime:    delayTime,
                durationTime: 0f,
                repeatCount:  repeatCount,
                intervalTime: intervalTime,
                action:       action,
                callBack:     callBack);
        }

        public void AddLoop(
            float  intervalTime,
            Action action,
            float  delayTime = 0f)
        {
            AddTimerEvent(
                delayTime:    delayTime,
                durationTime: 0f,
                repeatCount:  -1,
                intervalTime: intervalTime,
                action:       action,
                callBack:     null);
        }

        public void AddLoopWithDuration(
            float  durationTime,
            float  intervalTime,
            Action action,
            float  delayTime = 0f)
        {
            AddTimerEvent(
                delayTime:    delayTime,
                durationTime: durationTime,
                repeatCount:  -1,
                intervalTime: intervalTime,
                action:       action,
                callBack:     null);
        }

        // ── 核心 Tick ───────────────────────────────────────

        private void Tick()
        {
            for (int i = _events.Count - 1; i >= 0; i--)
            {
                var evt = _events[i];
                UpdateEvent(evt);

                if (evt.State == TimerState.Finished)
                {
                    _events.RemoveAt(i);
                    _pool.Return(evt);
                }
            }
        }

        private static void UpdateEvent(TimerEvent evt)
        {
            if (evt.State == TimerState.Finished)
            {
                TryInvokeCallBack(evt);
                return;
            }

            if (!evt.CanExecute) return;

            if (evt.State == TimerState.NotStarted)
            {
                evt.State          = TimerState.Running;
                evt.PhaseStartTime = Time.time;
            }

            evt.ElapsedTime += Time.deltaTime;

            float now = Time.time;

            // 阶段 1：等待延迟
            if (!evt.DelayFinished)
            {
                if (now >= evt.PhaseStartTime + evt.DelayTime)
                {
                    evt.DelayFinished  = true;
                    evt.PhaseStartTime = now;
                }
                return;
            }

            // 阶段 2：持续执行
            if (!evt.ContinueFinished)
            {
                if (evt.durationTime == 0f)
                {
                    evt.Action?.Invoke();
                    evt.ExecutedCount++;
                    evt.ContinueFinished = true;
                    evt.PhaseStartTime   = now;
                }
                else if (now < evt.PhaseStartTime + evt.durationTime)
                {
                    evt.Action?.Invoke();
                }
                else
                {
                    evt.ExecutedCount++;
                    evt.ContinueFinished = true;
                    evt.PhaseStartTime   = now;
                }
                return;
            }

            // 检查执行次数
            bool infinite     = evt.RepeatCount < 0;
            bool reachedLimit = !infinite && evt.ExecutedCount >= evt.RepeatCount;

            if (reachedLimit)
            {
                TimerEvent.MarkFinished(evt);
                return;
            }

            // 阶段 3：等待间隔
            if (!evt.IntervalFinished)
            {
                if (now >= evt.PhaseStartTime + evt.IntervalTime)
                {
                    evt.IntervalFinished = true;
                    evt.PhaseStartTime   = now;
                }
                return;
            }

            // 开始下一轮
            evt.ContinueFinished = false;
            evt.IntervalFinished = false;
        }

        // ── 辅助方法 ────────────────────────────────────────

        private static void InitEvent(
            TimerEvent evt,
            float  delayTime,
            float  durationTime,
            int    repeatCount,
            float  intervalTime,
            Action action,
            Action callBack)
        {
            evt.DelayTime        = delayTime;
            evt.durationTime     = durationTime;
            evt.RepeatCount      = repeatCount;
            evt.IntervalTime     = intervalTime;
            evt.Action           = action;
            evt.CallBack         = callBack;
            evt.State            = TimerState.NotStarted;
            evt.ElapsedTime      = 0f;
            evt.PhaseStartTime   = Time.time;
            evt.ExecutedCount    = 0;
            evt.CanExecute       = true;
            evt.DelayFinished    = false;
            evt.ContinueFinished = false;
            evt.IntervalFinished = false;
            evt.CallBackInvoked  = false;
        }

        private static void TryInvokeCallBack(TimerEvent evt)
        {
            if (evt.CallBackInvoked) return;
            evt.CallBackInvoked = true;
            evt.CallBack?.Invoke();
        }
    }

    // ── MonoBehaviour 驱动 ───────────────────────────────────
    public class TimerRunner : DesMonoSingleton<TimerRunner>
    {
        public event Action _timerEvent;
        private void Update() => _timerEvent?.Invoke();
    }
}