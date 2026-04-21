using System.Collections.Generic;
using QFramework.Enum;
using UnityEngine;

namespace QFramework.Model
{
    public interface IEnemeyConfigModel : IModel
    {
        // 只读类方法
        public EnemeyConfig GetEnemyFromCache(EnemyTypeEnum enemyEnum);  // 根据敌人枚举获取敌人
    }

    public class EnemeyConfigModel : AbstractModel, IEnemeyConfigModel
    {
        // 敌人配置列表
        private Dictionary<EnemyTypeEnum, EnemeyConfig> _enemeyModelsConfig = new Dictionary<EnemyTypeEnum, EnemeyConfig>()
        {
            {EnemyTypeEnum.Worker, new EnemeyConfig(5, 100, 100, 10)},
            {EnemyTypeEnum.Enemy2, new EnemeyConfig(10, 200, 200, 20)},
            {EnemyTypeEnum.Enemy3, new EnemeyConfig(15, 300, 300, 30)},
        };

        protected override void OnInit()
        {

        }

        // 只读类方法
        public EnemeyConfig GetEnemyFromCache(EnemyTypeEnum enemyEnum)
        {
            if(enemyEnum == EnemyTypeEnum.None)
            {
                Debug.LogError("敌人枚举为空");
                return null;
            }
            return _enemeyModelsConfig[enemyEnum];
        }
    }


    /// <summary>
    /// 由于每个敌人都是独立的运行时数据，所以配置model直接使用BindableProperty来存储
    /// </summary>
    public class EnemeyConfig
    {
        // 标识
        public EnemyTypeEnum EnemyType;

        // 敌人属性
        public int enemySize;
        public int MaxHealth;
        public int CurrentHealth;
        public int Speed;
        public EnemyState EnemyState;

        /// <summary>
        /// 初始化敌人配置
        /// </summary>
        public EnemeyConfig(int enemySize, int maxHealth, int currentHealth, int speed)
        {
            this.enemySize = enemySize;
            this.MaxHealth = maxHealth;
            this.CurrentHealth = currentHealth;
            this.Speed = speed;
        }
    }


    public enum EnemyState
    {
        Idle,
        Moving,
        Attacking,
        Dead,
    }
}