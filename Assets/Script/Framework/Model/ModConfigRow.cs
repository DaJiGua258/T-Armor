using System;
using QFramework.Enum;

namespace QFramework.Model
{
    /// <summary>
    /// ModConfig.xlsx 导出 JSON 的扁平行结构
    /// 加载后由 ModConfigModel 按 ItemType 分组重建 ModConfig
    /// </summary>
    [Serializable]
    public class ModConfigRow
    {
        public ItemTypeEnum ItemType;
        public ModCategory Category;
        public string Name;
        public string Description;
        public StatName EntryTarget;
        public float EntryValue;
        public ModOp EntryOp;
    }
}
