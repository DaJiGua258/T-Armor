using System.Collections.Generic;
using System.Linq;
using QFramework.Enum;
using UnityEngine;

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
            var rows = ConfigLoader.LoadFromJson<ModConfigRow>("Config/ModConfig");
            Debug.Log($"[ModConfig] 从 JSON 加载了 {rows.Count} 个词条");

            foreach (var group in rows.GroupBy(r => r.ItemType))
            {
                var first = group.First();
                var entries = group.Select(r => new ModEntry(r.EntryTarget, r.EntryValue, r.EntryOp)).ToList();
                _modConfigs[group.Key] = new ModConfig(group.Key, first.Category, entries);
                Debug.Log($"[ModConfig]   → {group.Key} ({first.Category}), {entries.Count} 个词条");
            }
        }

        public ModConfig GetModConfig(ItemTypeEnum itemType)
        {
            return _modConfigs.TryGetValue(itemType, out var config) ? config : null;
        }
    }
}
