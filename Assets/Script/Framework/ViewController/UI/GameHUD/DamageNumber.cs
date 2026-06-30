using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    /// <summary>
    /// 单个伤害数字 UI 组件。
    /// 挂载的 Canvas 必须设置为 World Space，
    /// 这样 transform.position 直接对应世界坐标，无需屏幕坐标转换。
    /// </summary>
    public class DamageNumber : MonoBehaviour
    {
        [SerializeField] private Text _text;
        [SerializeField] private CanvasGroup _canvasGroup;

        private Tweener _moveTweener;
        private Tweener _fadeTweener;
        private bool _isAvailable = true;

        // generation 用于取消过期的 DelayedCall 回调
        private int _generation;

        // 当前 Y 轴浮动值（世界坐标）
        private float _currentWorldY;

        // 屏外位置，用于隐藏未使用的对象
        private static readonly Vector3 OffScreenPos = new Vector3(-9999f, -9999f, 0f);

        public bool IsAvailable => _isAvailable;

        /// <summary>动画播完后由 Manager 回收</summary>
        public Action OnAnimationComplete;

        private void Awake()
        {
            _canvasGroup.alpha = 0f;
            transform.position = OffScreenPos;

            // ---- 移动 Tweener（操作世界坐标 Y）----
            _moveTweener = DOTween.To(
                    () => _currentWorldY,
                    v =>
                    {
                        _currentWorldY = v;
                        // 只更新 Y，保留 X/Z 不变
                        var pos = transform.position;
                        pos.y = v;
                        transform.position = pos;
                    },
                    0f, 1f)
                .SetEase(Ease.OutCubic)
                .SetAutoKill(false)
                .Pause();

            // ---- 淡入淡出 Tweener ----
            _fadeTweener = _canvasGroup
                .DOFade(0f, 1f)
                .SetAutoKill(false)
                .Pause();
        }

        /// <summary>
        /// 在指定世界坐标播放伤害数字动画。
        /// </summary>
        public void Show(float damage, Vector3 worldPos,
            float popUpOffset, float popUpDuration,
            float displayDuration, float fadeOutDuration,
            float randomXOffset, float randomYOffset)
        {
            _isAvailable = false;
            _generation++;
            int capturedGen = _generation;

            _text.text = Mathf.RoundToInt(damage).ToString();

            float offsetX = UnityEngine.Random.Range(-randomXOffset, randomXOffset);
            float offsetY = UnityEngine.Random.Range(-randomYOffset, randomYOffset);
            Vector3 targetPos = worldPos + new Vector3(offsetX, offsetY, 0f);
            Vector3 startPos  = new Vector3(targetPos.x, targetPos.y - popUpOffset, targetPos.z);

            // --- 初始化位置与透明度 ---
            transform.position = startPos;
            _currentWorldY     = startPos.y;
            _canvasGroup.alpha = 0f;

            // --- Phase 1：弹出 + 淡入（并行） ---
            // 修复：先 ChangeValues 再设回调再 Restart，保证 OnComplete 不被覆盖
            _moveTweener
                .ChangeValues(_currentWorldY, targetPos.y, popUpDuration)
                .OnComplete(null)
                .Restart();

            _fadeTweener
                .ChangeValues(0f, 1f, popUpDuration * 0.6f)
                .OnComplete(null)
                .Restart();

            // --- Phase 2：停留后淡出 ---
            float totalDelay = popUpDuration + displayDuration;
            DOVirtual.DelayedCall(totalDelay, () =>
            {
                // generation 不匹配说明该对象已被重用或重置，直接忽略
                if (capturedGen != _generation) return;

                // 修复：先 ChangeValues，再挂 OnComplete，再 Restart
                _fadeTweener
                    .ChangeValues(_canvasGroup.alpha, 0f, fadeOutDuration)
                    .OnComplete(OnFadeOutComplete)
                    .Restart();

            }, ignoreTimeScale: false);
        }

        private void OnFadeOutComplete()
        {
            _canvasGroup.alpha  = 0f;
            _currentWorldY      = OffScreenPos.y;
            transform.position  = OffScreenPos;
            _isAvailable        = true;
            OnAnimationComplete?.Invoke();
        }

        /// <summary>
        /// 强制重置到初始状态（供 Manager 回收时调用）。
        /// </summary>
        public void ResetState()
        {
            _generation++; // 使所有飞行中的 DelayedCall 失效

            if (_moveTweener != null && _moveTweener.IsPlaying()) _moveTweener.Pause();
            if (_fadeTweener  != null && _fadeTweener.IsPlaying())  _fadeTweener.Pause();

            // 清除淡出回调，避免回收后触发 OnAnimationComplete
            _fadeTweener?.OnComplete(null);

            _canvasGroup.alpha = 0f;
            _currentWorldY     = OffScreenPos.y;
            transform.position = OffScreenPos;
            _isAvailable       = true;
        }

        private void OnDestroy()
        {
            // 只 Kill 当前对象持有的 Tweener，避免误杀其他对象
            _moveTweener?.Kill();
            _fadeTweener?.Kill();
        }
    }
}