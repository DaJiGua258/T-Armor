using System.Collections.Generic;
using QFramework.Enum;

namespace QFramework.Model
{
    public interface IEnemeyDataModel : IModel
    {
        // 只读类方法
        public EnemeyConfigModel GetEnemyFromCache(int id); // 根据索引获取敌人

        // 可写类方法
        public int AddEnemyToCache(EnemyEnum enemyEnum); // 添加敌人到缓存，返回敌人索引
        public void DamageEnemy(int id, int damage); // 伤害敌人
        
    }

    public class EnemeyDataModel : AbstractModel, IEnemeyDataModel
    {
        // 敌人实例化列表缓存
        private List<EnemeyConfigModel> _enemeyModelCache = new List<EnemeyConfigModel>();

        // 敌人配置列表
        private Dictionary<EnemyEnum, EnemeyConfigModel> _enemeyModelsConfig = new Dictionary<EnemyEnum, EnemeyConfigModel>()
        {
            {EnemyEnum.Enemy1, new EnemeyConfigModel(5, 100, 100, 10)},
        };

        protected override void OnInit()
        {

        }

        // 只读类方法
        public EnemeyConfigModel GetEnemyFromCache(int id)
        {
            return _enemeyModelCache[id];
        }

        // 可写类方法
        public int AddEnemyToCache(EnemyEnum enemyEnum)
        {
            _enemeyModelCache.Add(_enemeyModelsConfig[enemyEnum]);
            return _enemeyModelCache.Count - 1;
        }

        public void DamageEnemy(int id, int damage)
        {
            var enemy = GetEnemyFromCache(id);
            enemy.CurrentHealth.Value -= damage;
            
        }
    }


    public class EnemeyConfigModel
    {
        public BindableProperty<int> enemySize { get; } = new BindableProperty<int>();
        public BindableProperty<int> MaxHealth { get; } = new BindableProperty<int>();
        public BindableProperty<int> CurrentHealth { get; } = new BindableProperty<int>();
        public BindableProperty<int> Speed { get; } = new BindableProperty<int>();

        /// <summary>
        /// 初始化敌人配置
        /// </summary>
        public EnemeyConfigModel(int enemySize, int maxHealth, int currentHealth, int speed)
        {
            this.enemySize.Value = enemySize;
            this.MaxHealth.Value = maxHealth;
            this.CurrentHealth.Value = currentHealth;
            this.Speed.Value = speed;
        }
    }
}