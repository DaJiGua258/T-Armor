using System.Collections.Generic;
using UnityEngine;
using QFramework;
using QFramework.Enum;
using QFramework.Model;
using System;

namespace QFramework.ViewController.UI.WeaponConfig
{
    public class WeaponSelectList : MonoBehaviour, IController
    {
        public Action<WeaponTypeEnum> OnWeaponConfirmed;

        [SerializeField] private GameObject _pf_weaponItem;
        [SerializeField] private Transform _contentRoot;

        private readonly List<WeaponItemsInfo> _weaponItems = new();

        private void Awake()
        {
            BuildList();
        }

        private void BuildList()
        {
            var model = this.GetModel<IWeaponConfigModel>();

            foreach (var type in model.WeaponConfigs.Keys)
            {
                if (type == WeaponTypeEnum.None) continue;
                CreateItem(type);
            }

            foreach (var type in model.HangerWeaponConfigs.Keys)
            {
                if (type == WeaponTypeEnum.None) continue;
                CreateItem(type);
            }
        }

        private void CreateItem(WeaponTypeEnum type)
        {
            var obj = Instantiate(_pf_weaponItem, _contentRoot);
            var item = obj.GetComponent<WeaponItemsInfo>();
            item.Init(type);
            item.Button.onClick.AddListener(() => OnItemClicked(item));
            item.gameObject.SetActive(false);
            _weaponItems.Add(item);
        }

        public void ShowWeapons(bool isHanger, WeaponTypeEnum selectedType)
        {
            var model = this.GetModel<IWeaponConfigModel>();
            var dict = isHanger ? model.HangerWeaponConfigs : model.WeaponConfigs;

            foreach (var item in _weaponItems)
            {
                var isTargetType = dict.ContainsKey(item.WeaponType);
                item.gameObject.SetActive(isTargetType);
                item.SetSelected(item.WeaponType == selectedType);
            }
        }

        private void OnItemClicked(WeaponItemsInfo clicked)
        {
            foreach (var item in _weaponItems)
                item.SetSelected(item == clicked);
            OnWeaponConfirmed?.Invoke(clicked.WeaponType);
        }

        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
    }
}
