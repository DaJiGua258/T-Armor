using DG.Tweening;
using QFramework.Event;
using QFramework.Model;
using QFramework.System;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class WeaponsPanel : BaseUIComponent
    {
        private WeaponSlot[] _slots;
        private WeaponDataModel[] _weaponDatas;
        private bool[] _prevReloading;

        void Awake()
        {
            var content = transform.Find("Content");
            if (content == null) return;

            _slots = new WeaponSlot[content.childCount];
            for (int i = 0; i < content.childCount; i++)
            {
                var child = content.GetChild(i);
                var numTxt = child.Find("Num/Txt");
                var reloadImg = child.Find("Reload/Img");
                _slots[i] = new WeaponSlot
                {
                    ammoTxt = numTxt ? numTxt.GetComponent<Text>() : null,
                    fillImg = reloadImg ? reloadImg.GetComponent<Image>() : null
                };
            }
        }

        void Start()
        {
            TypeEventSystem.Global.Register<WeaponInfoEvent.Register>(e => RegisterWeaponInfo())
                .UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void RegisterWeaponInfo()
        {
            _weaponDatas = new[]
            {
                PlayerSystem.PlayerWeapon.Left.Value,
                PlayerSystem.PlayerWeapon.Right.Value,
                PlayerSystem.PlayerWeapon.HangerLeft.Value,
                PlayerSystem.PlayerWeapon.HangerRight.Value,
            };
            _prevReloading = new bool[_weaponDatas.Length];

            for (int i = 0; i < _slots.Length && i < _weaponDatas.Length; i++)
            {
                RegisterSlot(_slots[i], _weaponDatas[i]);
            }
        }

        private void Update()
        {
            if (_weaponDatas == null) return;
            // 检测换弹状态变化，启动/停止换弹动画
            for (int i = 0; i < _slots.Length && i < _weaponDatas.Length; i++)
            {
                var data = _weaponDatas[i];
                if (data == null) continue;
                bool reloading = data.WeaponState == WeaponStateEnum.Reloading;
                if (reloading != _prevReloading[i])
                {
                    _prevReloading[i] = reloading;
                    if (reloading)
                        StartReloadAnimation(_slots[i], data);
                    else
                        _slots[i]?.FillTweener?.Kill();
                }
            }
        }

        private void RegisterSlot(WeaponSlot slot, WeaponDataModel data)
        {
            if (data == null) return;

            data.CurMagazine.Register(_ => OnMagazineChanged(slot, data))
                .UnRegisterWhenGameObjectDestroyed(gameObject);
            UpdateSlot(slot, data);
        }

        private void OnMagazineChanged(WeaponSlot slot, WeaponDataModel data)
        {
            slot.FillTweener?.Kill();
            UpdateSlot(slot, data);
        }

        private void UpdateSlot(WeaponSlot slot, WeaponDataModel data)
        {
            if (slot == null) return;
            if (slot.ammoTxt != null)
                slot.ammoTxt.text = $"{data.CurMagazine.Value:D3} / {data.MaxMagazine:D3}";

            if (slot.fillImg != null)
                slot.fillImg.fillAmount = (float)data.CurMagazine.Value / data.MaxMagazine;
        }

        private void StartReloadAnimation(WeaponSlot slot, WeaponDataModel data)
        {
            if (slot?.fillImg == null) return;
            slot.FillTweener?.Kill();
            slot.fillImg.fillAmount = 0f;
            slot.FillTweener = slot.fillImg
                .DOFillAmount(1f, data.ReloadTime)
                .SetEase(Ease.Linear);
        }
    }
}

[global::System.Serializable]
public class WeaponSlot
{
    public Text ammoTxt;
    public Image fillImg;
    [global::System.NonSerialized] public Tweener FillTweener;
}
