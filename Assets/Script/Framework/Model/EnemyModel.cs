using System;
using System.Collections.Generic;
using QFramework.Enum;
using UnityEngine;

namespace QFramework.Model
{
    public interface IEnemeyConfigModel : IModel
    {
        public EnemeyConfig GetEnemyFromCache(EnemyTypeEnum enemyEnum);
    }

    public class EnemeyConfigModel : AbstractModel, IEnemeyConfigModel
    {
        private Dictionary<EnemyTypeEnum, EnemeyConfig> _enemeyModelsConfig = new();

        protected override void OnInit()
        {
            // 从 Config/EnemyConfig.json 加载敌人配置
            var enemies = ConfigLoader.LoadFromJson<EnemeyConfig>("Config/EnemyConfig");
            Debug.Log($"[EnemyConfig] 从 JSON 加载了 {enemies.Count} 个敌人");
            foreach (var e in enemies)
            {
                e.PostLoad();
                _enemeyModelsConfig[e.EnemyType] = e;
                Debug.Log($"[EnemyConfig]   → EnemyType={e.EnemyType}, HP={e.MaxHealth}");
            }
        }

        public EnemeyConfig GetEnemyFromCache(EnemyTypeEnum enemyEnum)
        {
            if (enemyEnum == EnemyTypeEnum.None)
            {
                Debug.LogError("敌人枚举为空");
                return null;
            }
            return _enemeyModelsConfig[enemyEnum];
        }
    }

    [Serializable]
    public class EnemeyConfig
    {
        // 标识（从 JSON 反序列化，修复此前构造器未赋值的 bug）
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
        public float StopRange;
        public float ShootAccuracy;
        public int BulletSpeed = 20;
        public float MoveSpeed;
        public float AttackCooldown;

        // 异常状态阈值
        public float KnockbackThreshold;  // TODO: 已禁用
        public float BurnThreshold;  // TODO: 已禁用
        public float SlowThreshold;

        // JsonUtility 反序列化需要无参构造器
        public EnemeyConfig() { }

        /// <summary>反序列化后调用</summary>
        public void PostLoad()
        {
            KnockbackThreshold = 0;  // 击退已禁用
            BurnThreshold = 0;  // 灼烧已禁用
        }

        /// <summary>
        /// 初始化敌人配置（旧代码兼容，后续可移除）
        /// </summary>
        public EnemeyConfig(
            int enemySize, int maxHealth, float reactionTime, int speed, int damage,
            float detectionRange, float attackMaxRange, float attackMinRange, float moveSpeed,
            float stopRange = 1.5f, float shootAccuracy = 1f, int bulletSpeed = 20,
            float knockbackThreshold = 0.5f, float burnThreshold = 0.5f, float slowThreshold = 0.5f)
        {
            this.enemySize = enemySize;
            this.MaxHealth = maxHealth;
            this.ReactionTime = reactionTime;
            this.Speed = speed;
            this.Damage = damage;
            this.DetectionRange = detectionRange;
            this.AttackMaxRange = attackMaxRange;
            this.AttackMinRange = attackMinRange;
            this.StopRange = stopRange;
            this.ShootAccuracy = shootAccuracy;
            this.BulletSpeed = bulletSpeed;
            this.MoveSpeed = moveSpeed;
            this.KnockbackThreshold = knockbackThreshold;
            this.BurnThreshold = burnThreshold;
            this.SlowThreshold = slowThreshold;
        }
    }
}