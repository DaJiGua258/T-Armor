using System.Collections.Generic;
using QFramework.Enum;

namespace QFramework.Model
{
    public class ModEntry
    {
        public StatName Target;
        public float Value;
        public ModOp Operator;

        public ModEntry(StatName target, float value, ModOp op)
        {
            Target = target;
            Value = value;
            Operator = op;
        }
    }

    public class ModConfig
    {
        public ItemTypeEnum ItemType;
        public ModCategory Category;
        public List<ModEntry> Entries;

        public ModConfig(ItemTypeEnum itemType, ModCategory category, List<ModEntry> entries)
        {
            ItemType = itemType;
            Category = category;
            Entries = entries;
        }
    }

    public class ModData
    {
        public ItemTypeEnum ItemType;
        public List<ModEntry> Entries;

        public ModData(ItemTypeEnum itemType, List<ModEntry> entries)
        {
            ItemType = itemType;
            Entries = entries;
        }
    }

    public interface IModConfigModel : IModel
    {
        ModConfig GetModConfig(ItemTypeEnum itemType);
    }

    public class ModConfigModel : AbstractModel, IModConfigModel
    {
        private Dictionary<ItemTypeEnum, ModConfig> _modConfigs = new();

        protected override void OnInit()
        {
            AddConfig(ItemTypeEnum.Mod_Damage, ModCategory.Weapon, new()
                {
                    new(StatName.BulletDamage, 3, ModOp.Add)
                });

            AddConfig(ItemTypeEnum.Mod_Rpm, ModCategory.Weapon, new()
                {
                    new(StatName.Rpm, 0.90f, ModOp.Mul)
                });

            AddConfig(ItemTypeEnum.Mod_Homing, ModCategory.Weapon, new()
                {
                    new(StatName.EnableHoming, 1f, ModOp.Set),
                    new(StatName.BulletSpeed, -0.30f, ModOp.Mul),
                    new(StatName.SpreadAngle, 1f, ModOp.Mul),
                });
        }

        private void AddConfig(ItemTypeEnum itemType, ModCategory category, List<ModEntry> entries)
        {
            _modConfigs[itemType] = new ModConfig(itemType, category, entries);
        }

        public ModConfig GetModConfig(ItemTypeEnum itemType)
        {
            return _modConfigs.TryGetValue(itemType, out var config) ? config : null;
        }
    }
}
