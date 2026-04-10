using System;
using System.Collections.Generic;
using QFramework.Enum;
using QFramework.Model;


namespace QFramework.System
{
    public interface IEnemyInstanceSystem : ISystem
    {
        public int AddEnemy(EnemyTypeEnum enemyEnum);

        public void DamageEnemy(int id, int damage);
    }

    public class EnemyInstanceSystem : AbstractSystem, IEnemyInstanceSystem
    {
        /// <summary>
        /// 世界中的所有实例化敌人数据缓存
        /// </summary>
        private Dictionary<int, EnemeyDataModel> _enemyDataCache = new Dictionary<int, EnemeyDataModel>();
        private int _enemyCounter = 0;
        
        private IEnemeyConfigModel _enemeyConfigModel => this.GetModel<IEnemeyConfigModel>();

        /// <summary>
        /// 添加敌人，返回敌人id
        /// </summary>
        public int AddEnemy(EnemyTypeEnum enemyEnum)
        {
            var enemyConfig = _enemeyConfigModel.GetEnemyFromCache(enemyEnum);
            EnemeyDataModel enemyData = new EnemeyDataModel(enemyConfig);
            int currentId = enemyData.InstanceId.Value + _enemyCounter;

            _enemyDataCache.Add(currentId, enemyData);
            _enemyCounter++;
            return currentId;
        }

        protected override void OnInit()
        {
            
        }

        public void DamageEnemy(int id, int damage)
        {
            _enemyDataCache[id].CurrentHealth.Value -= damage;
        }
    }

    /// <summary>
    /// 由于每个敌人都是独立的运行时数据，所以配置model直接使用BindableProperty来存储
    /// </summary>
    public class EnemeyDataModel : InstanceType
    {
        private static int _enemyCounter = 0;

        public EnemyState EnemyState;
        public BindableProperty<int> enemySize = new BindableProperty<int>();
        public BindableProperty<int> MaxHealth = new BindableProperty<int>();
        public BindableProperty<int> CurrentHealth = new BindableProperty<int>();
        public BindableProperty<int> Speed = new BindableProperty<int>();

        /// <summary>
        /// 依据传入的敌人枚举，选取敌人配置，进行实例化
        /// </summary>
        public EnemeyDataModel(EnemeyConfig enemeyConfig)
        {
            this.TypeEnum = TypeEnum.Enemy;
            this.InstanceId.Value = GetInstanceId((int)enemeyConfig.EnemyType, _enemyCounter);
            _enemyCounter++;

            this.enemySize.Value = enemeyConfig.enemySize;
            this.MaxHealth.Value = enemeyConfig.MaxHealth;
            this.CurrentHealth.Value = enemeyConfig.CurrentHealth;
            this.Speed.Value = enemeyConfig.Speed;
        }
    }


}