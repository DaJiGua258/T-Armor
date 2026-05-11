using System;
using QFramework.Event;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using QFramework.System;
using QFramework.Model;
using UnityEngine.Serialization;

namespace QFramework.ViewController.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class WeaponInfo : BaseUIComponent
    {

        private RectTransform _rectTransform;
        [SerializeField] private InfoItemSlider hangerLeft;
        [SerializeField] private InfoItemSlider hangerRight;
        [SerializeField] private InfoItemSlider sideLeft;
        [SerializeField] private InfoItemSlider sideRight;
        [SerializeField] private CanvasGroup leftGroup;
        [SerializeField] private CanvasGroup rightGroup;
        private WeaponDataModel _hangerLeftData;
        private WeaponDataModel _hangerRightData;
        private WeaponDataModel _sideLeftData;
        private WeaponDataModel _sideRightData;



        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        void Start()
        {
            foreach (var item in _rectTransform.GetComponentsInChildren<RectTransform>())
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(item);
            }

            TypeEventSystem.Global.Register<WeaponInfoEvent.Register>(e => RegisterWeaponInfo())
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            TypeEventSystem.Global.Register<PlayerEvent.SwitchAimingMode>(
                e =>
                {
                    float alpha = e.Mode == AimingModeEnum.Combat ? 1f : 0f;
                    leftGroup.alpha = alpha;
                    rightGroup.alpha = alpha;
                }
            ).UnRegisterWhenGameObjectDestroyed(gameObject);

            SetupTweener(hangerLeft);
            SetupTweener(hangerRight);
            SetupTweener(sideLeft);
            SetupTweener(sideRight);
        }

        private void SetupTweener(InfoItemSlider info)
        {
            info.Tweener = info.Img
                .DOFillAmount(1f, 0f)
                .SetAutoKill(false);
        }

        /// <summary>
        /// 由于武器数据创建时间比较晚，所以需要将这个方法注册为事件
        /// 用事件来通知另外一个事件的注册
        /// </summary>
        private void RegisterWeaponInfo()
        {
            _sideLeftData = PlayerSystem.PlayerWeapon.Left.Value;
            _sideRightData = PlayerSystem.PlayerWeapon.Right.Value;
            _hangerLeftData = PlayerSystem.PlayerWeapon.HangerLeft.Value;
            _hangerRightData = PlayerSystem.PlayerWeapon.HangerRight.Value;

            // sideLeft
            if (_sideLeftData != null)
            {
                _sideLeftData.CurMagazine.Register(_ => UpdateWeaponInfo(sideLeft, _sideLeftData));
                _sideLeftData.CurMaxAmmo.Register(_ =>
                {
                    if(_sideLeftData.WeaponState == WeaponStateEnum.Reloading) UpdateReloadTime(sideLeft, _sideLeftData);
                });
                UpdateWeaponInfo(sideLeft, _sideLeftData);
            }

            // sideRight
            if (_sideRightData != null)
            {
                _sideRightData.CurMagazine.Register(_ => UpdateWeaponInfo(sideRight, _sideRightData));
                _sideRightData.CurMaxAmmo.Register(_ =>
                {
                    if(_sideRightData.WeaponState == WeaponStateEnum.Reloading) UpdateReloadTime(sideRight, _sideRightData);
                });
                UpdateWeaponInfo(sideRight, _sideRightData);
            }

            // hangerLeft
            if (_hangerLeftData != null)
            {
                _hangerLeftData.CurMagazine.Register(_ => UpdateWeaponInfo(hangerLeft, _hangerLeftData));
                _hangerLeftData.CurMaxAmmo.Register(_ =>
                {
                    if(_hangerLeftData.WeaponState == WeaponStateEnum.Reloading) UpdateReloadTime(hangerLeft, _hangerLeftData);
                });
                UpdateWeaponInfo(hangerLeft, _hangerLeftData);
            }

            // hangerRight
            if (_hangerRightData != null)
            {
                _hangerRightData.CurMagazine.Register(_ => UpdateWeaponInfo(hangerRight, _hangerRightData));
                _hangerRightData.CurMaxAmmo.Register(_ =>
                {
                    if(_hangerRightData.WeaponState == WeaponStateEnum.Reloading) UpdateReloadTime(hangerRight, _hangerRightData);
                });
                UpdateWeaponInfo(hangerRight, _hangerRightData);
            }
        }

        private void UpdateWeaponInfo(InfoItemSlider info, WeaponDataModel data)
        {
            if (info.Txt != null)
                info.Txt.text = (data.CurMaxAmmo.Value + data.CurMagazine.Value).ToString();
            info.Img.fillAmount = (float)data.CurMagazine.Value / data.MaxMagazine;
        }

        private void UpdateReloadTime(InfoItemSlider info, WeaponDataModel data)
        {
            if (info.Txt != null)
                info.Txt.text = "装填";
            info.Img.fillAmount = 0f;
            info.Tweener.ChangeEndValue(1f, data.ReloadTime - 0.01f, true)
                .SetEase(Ease.Linear)
                .Restart();
        }
    }

        [Serializable]
    public class InfoItemSlider
    {
        public Image Img;
        public Text Txt;

        public Tweener Tweener;
        public Tweener CanvasTweener;

        [NonSerialized] public Tween HideDelayTween;
        [NonSerialized] public bool IsVisible;
    }
}
