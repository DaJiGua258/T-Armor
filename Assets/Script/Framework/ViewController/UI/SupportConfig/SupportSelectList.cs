using System;
using System.Collections.Generic;
using QFramework.Enum;
using QFramework.Model;
using UnityEngine;

namespace QFramework.ViewController.UI.SupportConfig
{
    public class SupportSelectList : MonoBehaviour, IController
    {
        public Action<SupportTypeEnum> OnItemConfirmed;

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
            var model = this.GetModel<ISupportConfigModel>();

            var supportTypes = new[]
            {
                SupportTypeEnum.AirStrikes,
                SupportTypeEnum.AirSupport,
                SupportTypeEnum.Artillery,
                SupportTypeEnum.Missile,
            };

            foreach (var type in supportTypes)
            {
                if (type == SupportTypeEnum.None) continue;
                if (model.GetSupportConfig(type) == null) continue;

                var obj = Instantiate(_pf_supportItem, _contentRoot);
                var item = obj.GetComponent<SupportItemInfo>();
                item.Init(type);
                item.Button.onClick.AddListener(() => OnItemClicked(item));
                item.gameObject.SetActive(false);
                _supportItems.Add(item);
            }
        }

        public void ShowItems(SupportTypeEnum selectedType)
        {
            foreach (var item in _supportItems)
            {
                item.gameObject.SetActive(true);
                item.SetSelected(item.SupportType == selectedType);
            }
        }

        private void OnItemClicked(SupportItemInfo clicked)
        {
            foreach (var item in _supportItems)
                item.SetSelected(item == clicked);
            OnItemConfirmed?.Invoke(clicked.SupportType);
        }
    }
}
