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
        }

        void Update()
        {

        }


        /// <summary>
        /// 由于武器数据创建时间比较晚，所以需要将这个方法注册为事件
        /// 用事件来通知另外一个事件的注册
        /// </summary>
        private void RegisterWeaponInfo()
        {
            var left = PlayerSystem.PlayerWeapon.WeaponDataLeft.Value;
            var right = PlayerSystem.PlayerWeapon.WeaponDataRight.Value;

            // 注册弹药数量事件
            left.CurrentMagazine.Register(e => 
            {
                if(e == 0) UpdateReloadTime();
                if(e == left.MaxMagazine) UpdateWeaponInfo();
            });

            right.CurrentMagazine.Register(e => 
            {
                if(e == 0) UpdateReloadTime();
                if(e == right.MaxMagazine) UpdateWeaponInfo();
            });

            left.CurrentAmmo.Register(e => UpdateWeaponInfo());
            right.CurrentAmmo.Register(e => UpdateWeaponInfo());


            UpdateWeaponInfo();
        }

        private void UpdateWeaponInfo()
        {
            var left = PlayerSystem.PlayerWeapon.WeaponDataLeft.Value;
            var right = PlayerSystem.PlayerWeapon.WeaponDataRight.Value;

            leftWeaponInfo.txt.text = left.CurrentAmmo.ToString();
            rightWeaponInfo.txt.text = right.CurrentAmmo.ToString();

        
            leftWeaponInfo.img.DOFillAmount((float)left.CurrentMagazine.Value / left.MaxMagazine, 0f);
            rightWeaponInfo.img.DOFillAmount((float)right.CurrentMagazine.Value / right.MaxMagazine, 0f);
        }

        private void UpdateReloadTime()
        {
            void Update(WeaponInfoItem info, WeaponDataModel data)
            {
                info.txt.text = "装填";
                info.img.fillAmount = 0f;
                info.img.DOFillAmount(1f, data.ReloadTime - 0.01f).SetEase(Ease.Linear);
            }
            
            var left = PlayerSystem.PlayerWeapon.WeaponDataLeft.Value;
            var right = PlayerSystem.PlayerWeapon.WeaponDataRight.Value;

            if(left.CurrentMagazine.Value == 0) Update(leftWeaponInfo, left);
            if(right.CurrentMagazine.Value == 0) Update(rightWeaponInfo, right);
        }
    }

    [Serializable]
    public class WeaponInfoItem
    {
        public Image img;
        public Text txt;
    }
}