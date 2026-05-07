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
            {ItemTypeEnum.None, new ItemConfig(ItemTypeEnum.None, "", false, 0, "", "")},
            // {ItemTypeEnum.Parts, new ItemConfig(ItemTypeEnum.Parts, true, 999)},
            {ItemTypeEnum.Supply_Health, new ItemConfig(
                    ItemTypeEnum.Supply_Health, "医疗箱", true, 3,
                        "Texture/UI/Icon/icon_health", "恢复生命值", true)},

            {ItemTypeEnum.Supply_Ammo, new ItemConfig(
                    ItemTypeEnum.Supply_Ammo, "弹药箱", true, 3,
                        "Texture/UI/Icon/icon_ammo", "恢复弹药", true)},

            {ItemTypeEnum.Marker_Artillery, new ItemConfig(
                    ItemTypeEnum.Marker_Artillery, "炮击指令", true, 3,
                        "Texture/UI/Icon/icon_marker_artillery", "在指定位置进行一连串的炮火支援", true)},
            
            {ItemTypeEnum.Marker_Missile, new ItemConfig(
                    ItemTypeEnum.Marker_Missile, "制导导弹", true, 3,
                        "Texture/UI/Icon/icon_missile", "在指定位置发射一枚制导导弹", true)},
                        
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
        public string name;
        public bool canStack;
        public int maxStack;
        public string iconPath;
        public string description;
        public bool canUse;

        public ItemConfig(ItemTypeEnum itemType, string name, bool canStack, int maxStack, string iconPath, string description, bool canUse = false)
        {
            this.ItemType = itemType;
            this.name = name;
            this.canStack = canStack;
            this.maxStack = maxStack;
            this.iconPath = iconPath;
            this.description = description;
            this.canUse = canUse;
        }
    }
}