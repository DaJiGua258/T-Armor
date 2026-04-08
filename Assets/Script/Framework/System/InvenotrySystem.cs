using System.Collections.Generic;
using QFramework.Enum;
using QFramework.Event;
using QFramework.Model;
using Unity.Collections;
using UnityEngine;

namespace QFramework.System
{
    public interface IInvenotrySystem : ISystem
    {
        // public void AddNewItemToInventory(ItemTypeEnum itemType);
        public List<ItemDataModel> ItemDataCache { get; }
        public void SetItemData(int slotIndex, ItemDataModel itemData);
        public void SetItemDataCount(int slotIndex, int count);
    }

    public class InvenotrySystem : AbstractSystem, IInvenotrySystem
    {
        // int为背包索引
        public List<ItemDataModel> ItemDataCache { get; private set; } = new List<ItemDataModel>(15);
        private IItemConfigModel _itemDataModel => this.GetModel<IItemConfigModel>();

        protected override void OnInit()
        {
            for(int i = 0; i < 15; i++)
            {
                ItemDataCache.Add(new ItemDataModel(_itemDataModel.GetItemConfig(ItemTypeEnum.None)));
            }

            AddItemToInventory(ItemTypeEnum.Supply_Ammo);
            AddItemToInventory(ItemTypeEnum.Supply_Health);
        }

        /// <summary>
        /// 依据物品类型实例化默认数量，并添加在背包中
        /// </summary>
        public void AddItemToInventory(ItemTypeEnum itemType)
        {
            for(int i = 0; i < ItemDataCache.Count; i++)
            {
                if(ItemDataCache[i].ItemType == ItemTypeEnum.None)
                {
                    ItemDataCache[i].CopyFrom(new ItemDataModel(_itemDataModel.GetItemConfig(itemType)));
                    return;
                }
            }
        }

        /// <summary>
        /// 设置对应物品槽的数据
        /// </summary>
        public void SetItemData(int slotIndex, ItemDataModel itemData)
        {
            ItemDataCache[slotIndex].CopyFrom(itemData);
        }

        /// <summary>
        /// 设置对应物品槽的数量
        /// </summary>
        public void SetItemDataCount(int slotIndex, int count)
        {
            ItemDataCache[slotIndex].Count.Value = count;
        }
    }

    public class ItemDataModel : InstanceType
    {
        private static int _itemCounter = 0;

        public ItemTypeEnum ItemType;
        public bool canStack;
        public int maxStack;
        public string iconPath;
        public BindableProperty<int> Count = new BindableProperty<int>();
        public string description;

        public ItemDataModel(ItemConfig itemConfig)
        {
            this.TypeEnum = TypeEnum.Item;
            this.InstanceId = GetInstanceId((int)itemConfig.ItemType, _itemCounter);
            _itemCounter++;

            this.ItemType = itemConfig.ItemType;
            this.canStack = itemConfig.canStack;
            this.maxStack = itemConfig.maxStack;
            this.iconPath = itemConfig.iconPath;
            this.description = itemConfig.description;
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
            ItemType = otherItemData.ItemType;
            canStack = otherItemData.canStack;
            maxStack = otherItemData.maxStack;
            iconPath = otherItemData.iconPath;
            description = otherItemData.description;
            Count.Value = otherItemData.Count.Value;
        }

        /// <summary>
        /// 交换内部值
        /// </summary>
        public void SwapWith(ItemDataModel otherItemData)
        {
            (ItemType, otherItemData.ItemType) = (otherItemData.ItemType, ItemType);
            (canStack, otherItemData.canStack) = (otherItemData.canStack, canStack);
            (maxStack, otherItemData.maxStack) = (otherItemData.maxStack, maxStack);
            (iconPath, otherItemData.iconPath) = (otherItemData.iconPath, iconPath);
            (description, otherItemData.description) = (otherItemData.description, description);

            (Count.Value, otherItemData.Count.Value) = (otherItemData.Count.Value, Count.Value);
        }        
    }

}