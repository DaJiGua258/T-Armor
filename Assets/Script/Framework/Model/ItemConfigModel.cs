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
            {ItemTypeEnum.Supply_Health, new ItemConfig(
                    ItemTypeEnum.Supply_Health, "医疗箱", true, 3,
                        "Texture/UI/Icon/icon_health", "恢复生命值", true)},

            {ItemTypeEnum.Supply_Ammo, new ItemConfig(
                    ItemTypeEnum.Supply_Ammo, "弹药箱", true, 3,
                        "Texture/UI/Icon/icon_ammo", "恢复弹药", true)},
        };

        private static readonly ItemTypeEnum[] ModTypes = new[]
        {
            ItemTypeEnum.Mod_Damage,
            ItemTypeEnum.Mod_Rpm,
            ItemTypeEnum.Mod_Homing,
            ItemTypeEnum.Mod_Spread,
            ItemTypeEnum.Mod_Penetration,
            ItemTypeEnum.Mod_Ammo,
            ItemTypeEnum.Mod_Reload,
        };

        private static string ModIconPath(ItemTypeEnum modType)
        {
            return $"Texture/UI/Icon/Mod/icon_{modType.ToString().Replace("Mod_", "")}";
        }

        // 硬编码默认值，当 JSON 中没有提供 Name/Description 时回退使用
        private static readonly Dictionary<ItemTypeEnum, (string name, string desc)> DefaultModMetas = new()
        {
            { ItemTypeEnum.Mod_Damage, ("伤害芯片", "提升武器伤害") },
            { ItemTypeEnum.Mod_Rpm, ("射速芯片", "提升武器射速") },
            { ItemTypeEnum.Mod_Homing, ("追踪芯片", "子弹自动追踪敌人") },
            { ItemTypeEnum.Mod_Spread, ("稳定芯片", "减少武器散射") },
            { ItemTypeEnum.Mod_Penetration, ("穿透芯片", "子弹穿透敌人") },
            { ItemTypeEnum.Mod_Ammo, ("扩容芯片", "增加弹匣容量") },
            { ItemTypeEnum.Mod_Reload, ("换弹芯片", "加快换弹速度") },
        };

        protected override void OnInit()
        {
            var modConfig = ((IBelongToArchitecture)this).GetArchitecture().GetModel<IModConfigModel>();

            foreach (var modType in ModTypes)
            {
                var name = modConfig.GetModName(modType);
                var desc = modConfig.GetModDescription(modType);

                // JSON 没有配置时回退到硬编码默认值
                if (string.IsNullOrEmpty(name) && DefaultModMetas.TryGetValue(modType, out var fallback))
                {
                    name = fallback.name;
                    desc = fallback.desc;
                }

                _itemConfigs[modType] = new ItemConfig(
                    modType, name ?? "", false, 1,
                    ModIconPath(modType), desc ?? "", false);
            }
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
