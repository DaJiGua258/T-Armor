using System.Collections.Generic;
using QFramework.Enum;
using Unity.Mathematics;
using UnityEngine;

namespace QFramework.Model
{
    public interface IItemConfigModel : IModel
    {
        public ItemConfig GetItemConfig(ItemTypeEnum itemType);
    }

    public class ItemConfigModel : AbstractModel, IItemConfigModel
    {
        private Dictionary<ItemTypeEnum, ItemConfig> _itemConfigs = new Dictionary<ItemTypeEnum, ItemConfig>()
        {
            {ItemTypeEnum.None, new ItemConfig(ItemTypeEnum.None, false, 0, "", "")},
            // {ItemTypeEnum.Parts, new ItemConfig(ItemTypeEnum.Parts, true, 999)},
            {ItemTypeEnum.Supply_Health, new ItemConfig(ItemTypeEnum.Supply_Health, true, 3, "Texture/UI/Icon/icon_health", "恢复生命值")},
            {ItemTypeEnum.Supply_Ammo, new ItemConfig(ItemTypeEnum.Supply_Ammo, true, 3, "Texture/UI/Icon/icon_ammo", "恢复弹药")},
            // {ItemTypeEnum.Beacon_AirStrikes, new ItemConfig(ItemTypeEnum.Beacon_AirStrikes, true, 3)},
            // {ItemTypeEnum.Beacon_AirSupport, new ItemConfig(ItemTypeEnum.Beacon_AirSupport, true, 3)},   
            // {ItemTypeEnum.Beacon_Shelling, new ItemConfig(ItemTypeEnum.Beacon_Shelling, true, 3)},
        };

        protected override void OnInit()
        {
            
        }

        public ItemConfig GetItemConfig(ItemTypeEnum itemType)
        {
            return _itemConfigs[itemType];
        }
    }

    public class ItemConfig
    {
        public ItemTypeEnum ItemType;
        public bool canStack;
        public int maxStack;
        public string iconPath;
        public string description;

        public ItemConfig(ItemTypeEnum itemType, bool canStack, int maxStack, string iconPath, string description)
        {
            this.ItemType = itemType;
            this.canStack = canStack;
            this.maxStack = maxStack;
            this.iconPath = iconPath;
            this.description = description;
        }
    }
}