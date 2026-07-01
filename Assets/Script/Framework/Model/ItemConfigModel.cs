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

            // Mod 芯片
            {ItemTypeEnum.Mod_Damage, new ItemConfig(
                    ItemTypeEnum.Mod_Damage, "伤害芯片", false, 1,
                        ModIconPath(ItemTypeEnum.Mod_Damage), "提升武器伤害", false)},

            {ItemTypeEnum.Mod_Rpm, new ItemConfig(
                    ItemTypeEnum.Mod_Rpm, "射速芯片", false, 1,
                        ModIconPath(ItemTypeEnum.Mod_Rpm), "提升武器射速", false)},

            {ItemTypeEnum.Mod_Homing, new ItemConfig(
                    ItemTypeEnum.Mod_Homing, "追踪芯片", false, 1,
                        ModIconPath(ItemTypeEnum.Mod_Homing), "子弹自动追踪敌人", false)},
        };

        private static string ModIconPath(ItemTypeEnum modType)
        {
            // Mod_Rpm → "Texture/UI/Icon/icon_mod_Rpm"
            return $"Texture/UI/Icon/Mod/icon_{modType.ToString().Replace("Mod_", "")}";
        }

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