using System;
using System.Collections.Generic;
using QFramework.Enum;
using QFramework.Model;
using UnityEngine;

namespace QFramework.ViewController.UI.SupportConfig
{
    public class SupportSelectList : MonoBehaviour, IController
    {
        public Action<ItemTypeEnum> OnItemConfirmed;

        [SerializeField] private GameObject _pf_supportItem;
        [SerializeField] private Transform _contentRoot;

        private readonly List<SupportItemInfo> _supportItems = new();

        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        private void Awake()
        {
            BuildList();
        }

        private void BuildList()
        {
            var model = this.GetModel<IItemConfigModel>();

            // 只显示支援信标
            var supportTypes = new[]
            {
                ItemTypeEnum.Marker_AirStrikes,
                ItemTypeEnum.Marker_AirSupport,
                ItemTypeEnum.Marker_Artillery,
                ItemTypeEnum.Marker_Missile,
            };

            foreach (var type in supportTypes)
            {
                if (type == ItemTypeEnum.None) continue;
                if (model.GetItemConfig(type) == null) continue;

                var obj = Instantiate(_pf_supportItem, _contentRoot);
                var item = obj.GetComponent<SupportItemInfo>();
                item.Init(type);
                item.Button.onClick.AddListener(() => OnItemClicked(item));
                item.gameObject.SetActive(false);
                _supportItems.Add(item);
            }
        }

        public void ShowItems(ItemTypeEnum selectedType)
        {
            foreach (var item in _supportItems)
            {
                item.gameObject.SetActive(true);
                item.SetSelected(item.ItemType == selectedType);
            }
        }

        private void OnItemClicked(SupportItemInfo clicked)
        {
            foreach (var item in _supportItems)
                item.SetSelected(item == clicked);
            OnItemConfirmed?.Invoke(clicked.ItemType);
        }
    }
}
