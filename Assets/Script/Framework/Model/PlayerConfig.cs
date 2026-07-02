using System;

namespace QFramework.Model
{
    /// <summary>
    /// PlayerConfig.xlsx 导出的玩家基础属性配置
    /// 由 PlayerModel 加载并应用
    /// </summary>
    [Serializable]
    public class PlayerConfig
    {
        public int MaxHealth;
        public int Speed;
        public int MaxFuel;
        public float FuelRecovery;
        public int DashCost;
        public int SprintCost;
        public int SprintSmooth;

        /// <summary>反序列化后调用</summary>
        public void PostLoad()
        {
        }
    }
}
