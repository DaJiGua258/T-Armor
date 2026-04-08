using System.Collections.Generic;
using QFramework.Enum;
using QFramework.Event;
using UnityEngine;

namespace QFramework.ViewController.UI
{
    public class BagPanel : BasePanel
    {
        private Transform _slotGrid;
        private Transform _viewer;
        private List<Slot> _slots = new List<Slot>();

        void Awake()
        {
            _slotGrid = transform.Find("Content/Grid");
            _viewer = transform.Find("Content/Viewer");

            for(int i = 0; i < _slotGrid.childCount; i++)
            {
                _slots.Add(_slotGrid.GetChild(i).GetComponent<Slot>());
            }
        }

        void Start()
        {
            UpdateSlots();
            // 注册刷新事件
            for (int i = 0; i < _invenotrySystem.ItemDataCache.Count; i++)
            {
                _invenotrySystem.ItemDataCache[i].Count.RegisterOnValueChanged(UpdateSlots);
            }
        }

        private void UpdateSlots()
        {
            // 从背包遍历物品数据
            for(int i = 0; i < _invenotrySystem.ItemDataCache.Count; i++)
            {
                var itemData = _invenotrySystem.ItemDataCache[i];
                if(itemData.ItemType != ItemTypeEnum.None)
                {
                    _slots[i].OnInit(itemData);
                    _slots[i].Show();
                }
                else
                {
                    _slots[i].OnHideIcon();
                }
            }
            
        }

    }
}