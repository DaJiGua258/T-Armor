using System.Collections.Generic;
using QFramework.Enum;
using QFramework.Event;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class BagPanel : AbstractBasePanel
    {
        private Transform _slotGrid;
        private List<Slot> _slots = new List<Slot>();
        private Viewer _viewer;

        void Awake()
        {
            _slotGrid = transform.Find("Content/Grid");
            _viewer = transform.Find("Content/Viewer").GetComponent<Viewer>();

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
                _invenotrySystem.ItemDataCache[i].InstanceId.RegisterOnValueChanged(UpdateSlots);
            }
        }

        private void UpdateSlots()
        {
            // 从背包遍历物品数据
            for(int i = 0; i < _invenotrySystem.ItemDataCache.Count; i++)
            {
                _slots[i].Index = i;

                var itemData = _invenotrySystem.ItemDataCache[i];
                _slots[i].UpdateSlot(itemData);
            }
            
        }

    }
}