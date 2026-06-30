using System;
using DG.Tweening;
using QFramework.Event;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class PlayerPanel : BaseUIComponent
    {
        private const float BarTweenDuration = 0.1f;

        [SerializeField] private SliderlBar _fuelBar;
        [SerializeField] private SliderlBar _healthBar;

        void Awake()
        {
            InitBar(_healthBar);
            InitBar(_fuelBar);
        }

        void Start()
        {
            PlayerModel.CurrentHealth.Register(_ => UpdateHealthBar())
                .UnRegisterWhenGameObjectDestroyed(gameObject);
            PlayerModel.CurrentFuel.Register(_ => UpdateFuelBar())
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            // 初始刷新数值
            UpdateFuelBar();
            UpdateHealthBar();
        }

        private void InitBar(SliderlBar bar)
        {
            bar.Tweener = bar.Img.DOFillAmount(0f, 0f).SetAutoKill(false);
        }

        private void UpdateHealthBar()
        {
            UpdateBar(
                _healthBar,
                PlayerModel.CurrentHealth.Value,
                PlayerModel.MaxHealth.Value);
        }

        private void UpdateFuelBar()
        {
            UpdateBar(
                _fuelBar,
                PlayerModel.CurrentFuel.Value,
                PlayerModel.MaxFuel.Value);
        }

        private void UpdateBar(SliderlBar bar, float current, int max)
        {
            if(bar.txt == null || bar.Img == null) return;

            bar.Tweener
                .ChangeEndValue(current / max, BarTweenDuration, true)
                .SetEase(Ease.Linear)
                .Restart();

            bar.txt.text = $"> {Mathf.RoundToInt(current):000}";
        }

    }

    [Serializable]
    public class SliderlBar
    {
        public Image Img;
        public Text txt;
        public Tweener Tweener;
    }
}