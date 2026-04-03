using JetBrains.Annotations;
using QFramework.Model;
using QFramework.System;
using QFramework.Enum;
using UnityEngine;

namespace QFramework.Command
{
    public class EnemyCommand
    {
        /// <summary>
        /// 添加敌人
        /// </summary>
        /// 
        public class Add : AbstractCommand<int>
        {
            private IEnemyInstanceSystem _enemeyInstanceSystem => this.GetSystem<IEnemyInstanceSystem>();
            private EnemyTypeEnum _enemyEnum;
            private int _id;

            public Add(EnemyTypeEnum enemyEnum, int id)
            {
                this._enemyEnum = enemyEnum;
                this._id = id;
            }
        
            protected override int OnExecute()
            {
                return _enemeyInstanceSystem.AddEnemy(_enemyEnum);
            }
        }

        /// <summary>
        /// 伤害敌人
        /// </summary>
        public class Damage : AbstractCommand
        {
            private IEnemyInstanceSystem _enemeyInstanceSystem => this.GetSystem<IEnemyInstanceSystem>();
            
            private int _id;
            private int _damage;
            

            public Damage(int id, int damage)
            {
                _id = id;
                _damage = damage;
            }

            protected override void OnExecute()
            {
                _enemeyInstanceSystem.DamageEnemy(_id, _damage);
                Debug.Log("敌人受到伤害：" + _damage);
            }
        }

    }
}
