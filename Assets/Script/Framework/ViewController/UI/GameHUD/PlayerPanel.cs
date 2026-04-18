using System;
using DG.Tweening;
using QFramework.Event;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class PlayerPanel : AbstractBasePanel
    {
        private RectTransform _rectTransform;
        [SerializeField] private SliderlBar _healthBar;
        [SerializeField] private SliderlBar _fuelBar;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        void Start()
        {
            PlayerModel.CurrentHealth.Register(e => UpdateHealthBar());
            PlayerModel.CurrentFuel.Register(e => UpdateFuelBar());

            TypeEventSystem.Global.Register<UpdatePos>(e => UpdatePos(e.Pos));
        }

        private void UpdatePos(Vector2 pos)
        {
            _healthBar.RectTransform.anchoredPosition = _healthBar.Offset;
            _fuelBar.RectTransform.anchoredPosition = _fuelBar.Offset;
            _rectTransform.DOMove(Camera.main.WorldToScreenPoint(pos), 0.1f).SetEase(Ease.Linear);
        }

        private void UpdateHealthBar()
        {
            _healthBar.Img.DOFillAmount(
                    (float)PlayerModel.CurrentHealth.Value / PlayerModel.MaxHealth.Value, 0.1f
                    ).SetEase(Ease.Linear);

            _healthBar.txt.text = PlayerModel.CurrentHealth.Value.ToString();
        }

        private void UpdateFuelBar()
        {
            _fuelBar.Img.DOFillAmount(
                (float)PlayerModel.CurrentFuel.Value / PlayerModel.MaxFuel.Value, 0.1f
                ).SetEase(Ease.Linear);

            _fuelBar.txt.text = PlayerModel.CurrentFuel.Value.ToString();
        }
    }

    [Serializable]
    public class SliderlBar
    {
        public Vector2 Offset;
        public RectTransform RectTransform;
        public Image Img;
        public Text txt;
    }
}