using System.Collections.Generic;
using QFramework.Enum;
using QFramework.Event;
using QFramework.Model;
using Unity.Collections;
using UnityEngine;

// 前向声明，避免循环依赖

namespace QFramework.System
{
    public interface IInvenotrySystem : ISystem
    {
        // public void AddNewItemToInventory(ItemTypeEnum itemType);
        public List<ItemDataModel> ItemDataCache { get; }
        public void AddItem(int slotIndex, ItemDataModel itemData);
        public void SetItemCount(int slotIndex, int count);
        public ItemDataModel GetInventoryItemByIndex(int slotIndex);
        public bool HasFreeSlot();
        public void AddItemToInventory(ItemTypeEnum itemType, int count);
    }

    public class InvenotrySystem : AbstractSystem, IInvenotrySystem
    {
        // int为背包索引
        public List<ItemDataModel> ItemDataCache { get; private set; } = new List<ItemDataModel>(14);
        private IItemConfigModel _itemDataModel => this.GetModel<IItemConfigModel>();
        private IModConfigModel _modConfigModel => this.GetModel<IModConfigModel>();

        protected override void OnInit()
        {
            for(int i = 0; i < 14; i++)
            {
                ItemDataCache.Add(new ItemDataModel(_itemDataModel.GetItemConfig(ItemTypeEnum.None)));
            }

            // 开局 Mod 芯片
            AddItemToInventory(ItemTypeEnum.Mod_Damage, 1);
            AddItemToInventory(ItemTypeEnum.Mod_Rpm, 1);
            AddItemToInventory(ItemTypeEnum.Mod_Rpm, 1);
            AddItemToInventory(ItemTypeEnum.Mod_Homing, 1);
        }

        /// <summary>
        /// 依据物品类型实例化默认数量，并添加在背包中
        /// </summary>
        public void AddItemToInventory(ItemTypeEnum itemType, int count)
        {
            for(int i = 0; i < ItemDataCache.Count; i++)
            {
                if(ItemDataCache[i].ItemType == ItemTypeEnum.None)
                {
                    ItemDataCache[i].CopyFrom(new ItemDataModel(_itemDataModel.GetItemConfig(itemType)));
                    ItemDataCache[i].Count.Value = count;

                    // 如果是 Mod 物品，附上词条数据
                    var modConfig = _modConfigModel.GetModConfig(itemType);
                    if (modConfig != null)
                    {
                        ItemDataCache[i].ModData = new ModData(modConfig.ItemType, new List<ModEntry>(modConfig.Entries));
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// 添加物品数据
        /// </summary>
        public void AddItem(int slotIndex, ItemDataModel itemData)
        {
            ItemDataCache[slotIndex].CopyFrom(itemData);
        }

        /// <summary>
        /// 设置对应物品槽的数量
        /// </summary>
        public void SetItemCount(int slotIndex, int count)
        {
            ItemDataCache[slotIndex].Count.Value = count;
        }

        public ItemDataModel GetInventoryItemByIndex(int slotIndex)
        {
            return ItemDataCache[slotIndex];
        }

        public bool HasFreeSlot()
        {
            foreach (var slot in ItemDataCache)
                if (slot.ItemType == ItemTypeEnum.None)
                    return true;
            return false;
        }
    }

    public class ItemDataModel : InstanceType
    {
        private static int _itemCounter = 0;

        public ItemTypeEnum ItemType;
        public string name;
        public bool canStack;
        public int maxStack;
        public string iconPath;
        public BindableProperty<int> Count = new BindableProperty<int>();
        public string description;
        public bool canUse;
        public ModData ModData;  // Mod 词条数据，非 Mod 物品为 null

        /// <summary>
        /// 创建空物品（用于武器/Player Mod 槽位初始化）
        /// </summary>
        public ItemDataModel()
        {
            this.TypeEnum = TypeEnum.Item;
            this.InstanceId.Value = -1;
            this.ItemType = ItemTypeEnum.None;
            this.name = "空";
            this.iconPath = string.Empty;
            this.description = string.Empty;
            this.Count.Value = 0;
        }

        public ItemDataModel(ItemConfig itemConfig)
        {
            this.TypeEnum = TypeEnum.Item;
            this.InstanceId.Value = GetInstanceId((int)itemConfig.ItemType, _itemCounter);
            _itemCounter++;

            this.ItemType = itemConfig.ItemType;
            this.name = itemConfig.name;
            this.canStack = itemConfig.canStack;
            this.maxStack = itemConfig.maxStack;
            this.iconPath = itemConfig.iconPath;
            this.description = itemConfig.description;
            this.canUse = itemConfig.canUse;
            if(ItemType != ItemTypeEnum.None)
            {
                this.Count.Value = 1;
            }
            else
            {
                this.Count.Value = 0;
            }
        }

        /// <summary>
        /// 复制内部值
        /// </summary>
        public void CopyFrom(ItemDataModel otherItemData)
        {
            InstanceId.Value = otherItemData.InstanceId.Value;

            ItemType = otherItemData.ItemType;
            name = otherItemData.name;
            canStack = otherItemData.canStack;
            maxStack = otherItemData.maxStack;
            iconPath = otherItemData.iconPath;
            description = otherItemData.description;
            canUse = otherItemData.canUse;
            Count.Value = otherItemData.Count.Value;
            ModData = otherItemData.ModData;
        }

        /// <summary>
        /// 交换内部值
        /// </summary>
        public void SwapWith(ItemDataModel otherItemData)
        {
            // 先交换普通字段（不触发事件），确保数据完全就绪后再触发刷新
            (TypeEnum, otherItemData.TypeEnum) = (otherItemData.TypeEnum, TypeEnum);
            (ItemType, otherItemData.ItemType) = (otherItemData.ItemType, ItemType);
            (name, otherItemData.name) = (otherItemData.name, name);
            (canStack, otherItemData.canStack) = (otherItemData.canStack, canStack);
            (maxStack, otherItemData.maxStack) = (otherItemData.maxStack, maxStack);
            (iconPath, otherItemData.iconPath) = (otherItemData.iconPath, iconPath);
            (description, otherItemData.description) = (otherItemData.description, description);
            (canUse, otherItemData.canUse) = (otherItemData.canUse, canUse);
            (ModData, otherItemData.ModData) = (otherItemData.ModData, ModData);

            // 最后交换 BindableProperty，此时所有数据已就绪，事件触发时 UI 读取的是完整正确的状态
            (InstanceId.Value, otherItemData.InstanceId.Value) = (otherItemData.InstanceId.Value, InstanceId.Value);
            (Count.Value, otherItemData.Count.Value) = (otherItemData.Count.Value, Count.Value);
        }        
    }

}