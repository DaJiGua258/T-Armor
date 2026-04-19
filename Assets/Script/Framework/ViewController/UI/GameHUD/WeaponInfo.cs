using System;
using QFramework.Event;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using QFramework.System;

namespace QFramework.ViewController.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class WeaponInfo : AbstractBasePanel
    {
        
        private RectTransform _rectTransform;
        [SerializeField] private WeaponInfoItem leftWeaponInfo;
        [SerializeField] private WeaponInfoItem rightWeaponInfo;
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

            TypeEventSystem.Global.Register<RegisterWeaponInfo>(e => RegisterWeaponInfo());

            leftWeaponInfo.Tweener = leftWeaponInfo.Img
                .DOFillAmount(1f, 0f)
                .SetEase(Ease.Unset)
                .SetAutoKill(false);
            rightWeaponInfo.Tweener = rightWeaponInfo.Img
                .DOFillAmount(1f, 0f)
                .SetEase(Ease.Unset)
                .SetAutoKill(false);
        }

        void Update()
        {
            if (_leftData != null && _leftData.IsReloading != _leftReloadingLastFrame)
            {
                _leftReloadingLastFrame = _leftData.IsReloading;
                if (_leftData.IsReloading) UpdateReloadTime(leftWeaponInfo, _leftData);
                else UpdateWeaponInfo(leftWeaponInfo, _leftData);
            }

            if (_rightData != null && _rightData.IsReloading != _rightReloadingLastFrame)
            {
                _rightReloadingLastFrame = _rightData.IsReloading;
                if (_rightData.IsReloading) UpdateReloadTime(rightWeaponInfo, _rightData);
                else UpdateWeaponInfo(rightWeaponInfo, _rightData);
            }
        }


        /// <summary>
        /// 由于武器数据创建时间比较晚，所以需要将这个方法注册为事件
        /// 用事件来通知另外一个事件的注册
        /// </summary>
        private void RegisterWeaponInfo()
        {
            _leftData = PlayerSystem.PlayerWeapon.WeaponDataLeft.Value;
            _rightData = PlayerSystem.PlayerWeapon.WeaponDataRight.Value;
            _leftReloadingLastFrame = _leftData.IsReloading;
            _rightReloadingLastFrame = _rightData.IsReloading;

            // 左手
            _leftData.CurMagazine.Register(_ => 
            {
                if(!_leftData.IsReloading) UpdateWeaponInfo(leftWeaponInfo, _leftData);
            });

            _leftData.CurMaxAmmo.Register(_ =>
            {
                if(!_leftData.IsReloading) UpdateWeaponInfo(leftWeaponInfo, _leftData);
            });

            // 右手
            _rightData.CurMagazine.Register(_ => 
            {
                if(!_rightData.IsReloading) UpdateWeaponInfo(rightWeaponInfo, _rightData);
            });

            _rightData.CurMaxAmmo.Register(_ =>
            {
                if(!_rightData.IsReloading) UpdateWeaponInfo(rightWeaponInfo, _rightData);
            });

            UpdateWeaponInfo(leftWeaponInfo, _leftData);
            UpdateWeaponInfo(rightWeaponInfo, _rightData);
        }

        private void UpdateWeaponInfo(WeaponInfoItem info, WeaponDataModel data)
        {
            info.Txt.text = (data.CurMaxAmmo.Value + data.CurMagazine.Value).ToString();
            info.Img.fillAmount = (float)data.CurMagazine.Value / data.MaxMagazine;
        }

        private void UpdateReloadTime(WeaponInfoItem info, WeaponDataModel data)
        {
            info.Txt.text = "装填";
            info.Img.fillAmount = 0f;
            info.Tweener.ChangeEndValue(1f, data.ReloadTime - 0.01f, true)
                .SetEase(Ease.Linear)
                .Restart();
        }
    }

        [Serializable]
    public class WeaponInfoItem
    {
        public Image Img;
        public Text Txt;
        public Tweener Tweener;
    }
}