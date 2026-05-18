using System.Collections.Generic;
using System.Text;
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
        [SerializeField] private Text _descriptionText;

        private List<Slot> _inventorySlots = new List<Slot>();
        private StringBuilder _sb = new StringBuilder();

        void Awake()
        {
            // 收集背包槽位
            for (int i = 0; i < _inventoryRoot.childCount; i++)
            {
                var slot = _inventoryRoot.GetChild(i).GetComponent<Slot>();
                slot.SlotType = SlotType.Bag;
                _inventorySlots.Add(slot);
            }
        }

        void Start()
        {
            UpdateSlots();

            // 注册物品描述事件
            TypeEventSystem.Global.Register<UpdateViewerEvent>(OnUpdateViewer)
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            // 注册背包刷新事件
            for (int i = 0; i < InvenotrySystem.ItemDataCache.Count; i++)
            {
                InvenotrySystem.ItemDataCache[i].Count.RegisterOnValueChanged(UpdateSlots);
                InvenotrySystem.ItemDataCache[i].InstanceId.RegisterOnValueChanged(UpdateSlots);
            }
        }

        private void OnUpdateViewer(UpdateViewerEvent e)
        {
            if (e.itemData == null || e.itemData.TypeEnum == TypeEnum.None)
            {
                _descriptionText.text = string.Empty;
                return;
            }

            _sb.Clear();
            string desc = e.itemData.description;
            if (string.IsNullOrEmpty(desc))
            {
                _descriptionText.text = string.Empty;
                return;
            }

            string[] lines = desc.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (i > 0) _sb.AppendLine();
                _sb.Append("> ");
                _sb.Append(line);
            }

            _descriptionText.text = _sb.ToString();
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
        }
    }
}