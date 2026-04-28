using UnityEngine;
using QFramework.Enum;
using System.Collections.Generic;
using QFramework.Utility;
using QFramework.System;
using QFramework.Event;

namespace QFramework.ViewController.UI
{
    public class SetRenderTextureItems : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        private IResourceLoad _resourceLoad => this.GetUtility<IResourceLoad>();
        private Camera _camera;

        private Dictionary<ItemTypeEnum, Vector3> _itemCameraPositions = new Dictionary<ItemTypeEnum, Vector3>();
        private Dictionary<ItemTypeEnum, GameObject> _renderTextureItems = new Dictionary<ItemTypeEnum, GameObject>();

        void Awake()
        {
            _camera = transform.Find("RTCamera").GetComponent<Camera>();
        }

        void Start()
        {
            TypeEventSystem.Global.Register<UpdateViewerEvent>(e => SetCamera(e.itemData))
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            _renderTextureItems.Add(ItemTypeEnum.Supply_Ammo, 
                Instantiate(_resourceLoad.Load<GameObject>("Prefab/PickUp/Supply/AmmoSupply")));
            _renderTextureItems.Add(ItemTypeEnum.Supply_Health, 
                Instantiate(_resourceLoad.Load<GameObject>("Prefab/PickUp/Supply/HealthSupply")));

            SetItemSeparation();
        }

        private void SetItemSeparation()
        {
            int xOffset = 0;

            // 设置默认位置
            _itemCameraPositions.Add(ItemTypeEnum.None, Vector3.zero);

            // 遍历初始化物品的位置
            foreach (var item in _renderTextureItems)
            {
                xOffset += 5;
                item.Value.transform.SetParent(transform);
                item.Value.transform.localPosition = new Vector3(xOffset, 0, -5);
                item.Value.GetComponent<PickUpItems>().CanShowing();
                _itemCameraPositions.Add(item.Key, item.Value.transform.localPosition);
                
            }
        }

        private void SetCamera(ItemDataModel itemData)
        {
            if(itemData.TypeEnum == TypeEnum.None)
            {
                return;
            }
            _camera.transform.localPosition = _itemCameraPositions[itemData.ItemType];
        }
    }
}