using System.Collections.Generic;
using QFramework.Enum;
using QFramework.Event;
using QFramework.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class BagPanel : BaseUIComponent
    {
        [SerializeField] private Transform _inventoryRoot;  // 背包 Slot 容器
        [SerializeField] private Transform _hotbarRoot;  // 快捷栏 Slot 容器

        private List<Slot> _inventorySlots = new List<Slot>();
        private List<Slot> _hotbarSlots = new List<Slot>();

        void Awake()
        {
            // 收集背包槽位
            for (int i = 0; i < _inventoryRoot.childCount; i++)
            {
                var slot = _inventoryRoot.GetChild(i).GetComponent<Slot>();
                slot.SlotType = SlotType.Bag;
                _inventorySlots.Add(slot);
            }

            // 收集快捷栏槽位
            for (int i = 0; i < _hotbarRoot.childCount; i++)
            {
                var slot = _hotbarRoot.GetChild(i).GetComponent<Slot>();
                slot.SlotType = SlotType.Hotbar;
                _hotbarSlots.Add(slot);
            }
        }

        void Start()
        {
            UpdateSlots();

            // 注册背包刷新事件
            for (int i = 0; i < InvenotrySystem.ItemDataCache.Count; i++)
            {
                InvenotrySystem.ItemDataCache[i].Count.RegisterOnValueChanged(UpdateSlots);
                InvenotrySystem.ItemDataCache[i].InstanceId.RegisterOnValueChanged(UpdateSlots);
            }

            // 注册快捷栏刷新事件
            for (int i = 0; i < InvenotrySystem.SupportItemCache.Count; i++)
            {
                var item = InvenotrySystem.GetHotbarItemByIndex(i);
                item.Count.RegisterOnValueChanged(UpdateSlots);
                item.InstanceId.RegisterOnValueChanged(UpdateSlots);
            }
        }

        // 刷新所有槽位
        private void UpdateSlots()
        {
            // 更新背包槽位
            for (int i = 0; i < InvenotrySystem.ItemDataCache.Count; i++)
            {
                _inventorySlots[i].Index = i;
                _inventorySlots[i].UpdateSlot(InvenotrySystem.ItemDataCache[i]);
            }

            // 更新快捷栏槽位
            for (int i = 0; i < InvenotrySystem.SupportItemCache.Count; i++)
            {
                _hotbarSlots[i].Index = i;
                _hotbarSlots[i].UpdateSlot(InvenotrySystem.GetHotbarItemByIndex(i));
            }
        }
    }
}