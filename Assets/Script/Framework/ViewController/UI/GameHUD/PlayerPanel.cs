using System;
using DG.Tweening;
using QFramework.Event;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class PlayerPanel : AbstractBasePanel
    {
        private const float BarTweenDuration = 0.1f;
        private const float FadeInDuration = 0.15f;
        private const float FadeOutDuration = 0.2f;
        private const float HideDelay = 2f;

        private RectTransform _rectTransform;

        [SerializeField] private SliderlBar _healthBar;
        [SerializeField] private SliderlBar _fuelBar;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();

            InitBar(_healthBar);
            InitBar(_fuelBar);
        }

        void Start()
        {
            PlayerModel.CurrentHealth.Register(_ => UpdateHealthBar(false));
            PlayerModel.CurrentFuel.Register(_ => UpdateFuelBar(false));

            // 初始刷新数值，不触发淡入
            UpdateFuelBar(false);
            UpdateHealthBar(false);

            TypeEventSystem.Global.Register<WeaponInfoEvent.UpdatePos>(e => UpdatePos(e.Pos))
                .UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void InitBar(SliderlBar bar)
        {
            bar.Tweener = bar.Img.DOFillAmount(0f, 0f).SetAutoKill(false);

            if (bar.CanvasGroup != null)
            {
                bar.CanvasTweener = bar.CanvasGroup.DOFade(1f, 0f).SetAutoKill(false);
                bar.CanvasGroup.alpha = 0f;
                bar.IsVisible = false;
            }
        }

        private void UpdatePos(Vector2 pos)
        {
            _healthBar.RectTransform.anchoredPosition = _healthBar.Offset;
            _fuelBar.RectTransform.anchoredPosition = _fuelBar.Offset;
            _rectTransform.DOMove(Camera.main.WorldToScreenPoint(pos), BarTweenDuration).SetEase(Ease.Linear);
        }

        private void UpdateHealthBar(bool touchVisibility = true)
        {
            UpdateBar(
                _healthBar,
                PlayerModel.CurrentHealth.Value,
                PlayerModel.MaxHealth.Value,
                touchVisibility);
        }

        private void UpdateFuelBar(bool touchVisibility = true)
        {
            UpdateBar(
                _fuelBar,
                PlayerModel.CurrentFuel.Value,
                PlayerModel.MaxFuel.Value,
                touchVisibility);
        }

        private void UpdateBar(SliderlBar bar, float current, int max, bool touchVisibility)
        {
            bar.Tweener
                .ChangeEndValue(current / max, BarTweenDuration, true)
                .SetEase(Ease.Linear)
                .Restart();

            bar.txt.text = current.ToString();

            if (touchVisibility)
            {
                TouchBarVisibility(bar);
            }
        }

        private void TouchBarVisibility(SliderlBar bar)
        {
            if (bar.CanvasGroup == null || bar.CanvasTweener == null)
            {
                return;
            }

            // 每次触发都重置“2秒后淡出”
            if (bar.HideDelayTween != null && bar.HideDelayTween.IsActive())
            {
                bar.HideDelayTween.Kill();
            }

            // 仅在隐藏状态时淡入，连续触发不重复淡入
            if (!bar.IsVisible)
            {
                bar.CanvasTweener
                    .ChangeEndValue(1f, FadeInDuration, true)
                    .SetEase(Ease.OutQuad)
                    .Restart();

                bar.IsVisible = true;
            }

            bar.HideDelayTween = DOVirtual.DelayedCall(HideDelay, () =>
            {
                bar.CanvasTweener
                    .ChangeEndValue(0f, FadeOutDuration, true)
                    .SetEase(Ease.InQuad)
                    .Restart();

                bar.IsVisible = false;
            });
        }

        private void OnDestroy()
        {
            KillBarRuntimeTween(_healthBar);
            KillBarRuntimeTween(_fuelBar);
        }

        private void KillBarRuntimeTween(SliderlBar bar)
        {
            if (bar.HideDelayTween != null && bar.HideDelayTween.IsActive())
            {
                bar.HideDelayTween.Kill();
            }
        }
    }

    [Serializable]
    public class SliderlBar
    {
        public Vector2 Offset;
        public RectTransform RectTransform;
        public Image Img;
        public Text txt;
        public CanvasGroup CanvasGroup;
        public Tweener Tweener;

        [NonSerialized] public Tweener CanvasTweener;
        [NonSerialized] public Tween HideDelayTween;
        [NonSerialized] public bool IsVisible;
    }
}