using System;
using QFramework.Event;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using QFramework.System;
using QFramework.Model;

namespace QFramework.ViewController.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class WeaponInfo : BaseUIComponent
    {
        
        private RectTransform _rectTransform;
        [SerializeField] private InfoItemSlider leftWeaponInfo;
        [SerializeField] private InfoItemSlider rightWeaponInfo;
        private WeaponDataModel _leftData;
        private WeaponDataModel _rightData;
        private bool _leftReloadingLastFrame;
        private bool _rightReloadingLastFrame;



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
                    leftWeaponInfo.CanvasGroup.alpha = alpha;
                    rightWeaponInfo.CanvasGroup.alpha = alpha;
                }
            ).UnRegisterWhenGameObjectDestroyed(gameObject);

            leftWeaponInfo.Tweener = leftWeaponInfo.Img
                .DOFillAmount(1f, 0f)
                .SetAutoKill(false);
            rightWeaponInfo.Tweener = rightWeaponInfo.Img
                .DOFillAmount(1f, 0f)
                .SetAutoKill(false);

            // leftWeaponInfo.CanvasTweener = leftWeaponInfo.CanvasGroup
            //     .DOFade(1f, 0f)
            //     .SetAutoKill(false);
            // rightWeaponInfo.CanvasTweener = rightWeaponInfo.CanvasGroup
            //     .DOFade(1f, 0f)
            //     .SetAutoKill(false);

             // Start() 初始化为隐藏（避免开局就显示）
            // leftWeaponInfo.CanvasGroup.alpha = 0f;
            // rightWeaponInfo.CanvasGroup.alpha = 0f;
            // leftWeaponInfo.IsVisible = false;
            // rightWeaponInfo.IsVisible = false;
        }

        void Update()
        {
            // if (_leftData != null && _leftData.IsReloading != _leftReloadingLastFrame)
            // {
            //     _leftReloadingLastFrame = _leftData.IsReloading;
            //     if (_leftData.IsReloading) UpdateReloadTime(leftWeaponInfo, _leftData);
            //     else UpdateWeaponInfo(leftWeaponInfo, _leftData);
            // }

            // if (_rightData != null && _rightData.IsReloading != _rightReloadingLastFrame)
            // {
            //     _rightReloadingLastFrame = _rightData.IsReloading;
            //     if (_rightData.IsReloading) UpdateReloadTime(rightWeaponInfo, _rightData);
            //     else UpdateWeaponInfo(rightWeaponInfo, _rightData);
            // }
        }


        /// <summary>
        /// 由于武器数据创建时间比较晚，所以需要将这个方法注册为事件
        /// 用事件来通知另外一个事件的注册
        /// </summary>
        private void RegisterWeaponInfo()
        {
            _leftData = PlayerSystem.PlayerWeapon.Left.Value;
            _rightData = PlayerSystem.PlayerWeapon.Right.Value;
            // _leftReloadingLastFrame = _leftData.IsReloading;
            // _rightReloadingLastFrame = _rightData.IsReloading;

            // 左手
            _leftData.CurMagazine.Register(_ => UpdateWeaponInfo(leftWeaponInfo, _leftData));

            _leftData.CurMaxAmmo.Register(_ =>
            {
                if(_leftData.WeaponState == WeaponStateEnum.Reloading) UpdateReloadTime(leftWeaponInfo, _leftData);
            });

            // 右手
            _rightData.CurMagazine.Register(_ => UpdateWeaponInfo(rightWeaponInfo, _rightData));

            _rightData.CurMaxAmmo.Register(_ =>
            {
                if(_rightData.WeaponState == WeaponStateEnum.Reloading) UpdateReloadTime(rightWeaponInfo, _rightData);
            });

            UpdateWeaponInfo(leftWeaponInfo, _leftData);
            UpdateWeaponInfo(rightWeaponInfo, _rightData);
        }

        private void UpdateWeaponInfo(InfoItemSlider info, WeaponDataModel data)
        {
            info.Txt.text = (data.CurMaxAmmo.Value + data.CurMagazine.Value).ToString();
            info.Img.fillAmount = (float)data.CurMagazine.Value / data.MaxMagazine;
            
            // TouchWeaponInfoVisibility(info);
        }

        private void UpdateReloadTime(InfoItemSlider info, WeaponDataModel data)
        {
            info.Txt.text = "装填";
            info.Img.fillAmount = 0f;
            info.Tweener.ChangeEndValue(1f, data.ReloadTime - 0.01f, true)
                .SetEase(Ease.Linear)
                .Restart();
        }

        private void TouchWeaponInfoVisibility(InfoItemSlider info)
        {
            // 每次触发都重置“2秒后淡出”
            if (info.HideDelayTween != null && info.HideDelayTween.IsActive())
            {
                info.HideDelayTween.Kill();
            }
            // 仅在当前隐藏时淡入一次，连续开火不重复淡入
            if (!info.IsVisible)
            {
                info.CanvasTweener
                    .ChangeEndValue(1f, 0.15f, true)
                    .SetEase(Ease.OutQuad)
                    .Restart();
                info.IsVisible = true;
            }
            info.HideDelayTween = DOVirtual.DelayedCall(2f, () =>
            {
                info.CanvasTweener
                    .ChangeEndValue(0f, 0.2f, true)
                    .SetEase(Ease.InQuad)
                    .Restart();
                info.IsVisible = false;
            });
        }
    }

        [Serializable]
    public class InfoItemSlider
    {
        public Image Img;
        public Text Txt;
        public CanvasGroup CanvasGroup;

        public Tweener Tweener;
        public Tweener CanvasTweener;

        [NonSerialized] public Tween HideDelayTween;
        [NonSerialized] public bool IsVisible;
    }
}