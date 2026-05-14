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
            {EnemyTypeEnum.Worker, new EnemeyConfig(5, 50, 0.5f, 10, 5, 15f, 2f, 1f, 3f)},
            {EnemyTypeEnum.Warrior_AR, new EnemeyConfig(10, 100, 0.5f, 20, 20, 15f, 8f, 3f, 3f)},

            {EnemyTypeEnum.Raider, new EnemeyConfig(15, 50, 0.5f, 30, 30, 20f, 5f, 2f, 3f)},

            {EnemyTypeEnum.Dropper_Mid, new EnemeyConfig(15, 10, 1f, 30, 30, 20f, 5f, 5f, 3f)},
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
        public int Speed;
        public int Damage;

        // 状态参数
        public float ReactionTime;

        // 感知与战斗参数
        public float DetectionRange;
        public float AttackMaxRange;
        public float AttackMinRange;
        public float MoveSpeed;

        /// <summary>
        /// 初始化敌人配置
        /// </summary>
        public EnemeyConfig(
            int enemySize, int maxHealth, float reactionTime, int speed, int damage,
            float detectionRange, float attackMaxRange, float attackMinRange, float moveSpeed)
        {
            this.enemySize = enemySize;
            this.MaxHealth = maxHealth;
            this.ReactionTime = reactionTime;
            this.Speed = speed;
            this.Damage = damage;
            this.DetectionRange = detectionRange;
            this.AttackMaxRange = attackMaxRange;
            this.AttackMinRange = attackMinRange;
            this.MoveSpeed = moveSpeed;
        }
    }

}