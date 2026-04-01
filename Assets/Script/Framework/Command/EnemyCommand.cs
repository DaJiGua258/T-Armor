using JetBrains.Annotations;
using QFramework.Enum;
using QFramework.Model;

namespace QFramework.Command
{
    public class EnemyCommand
    {
        public class Add : AbstractCommand<int>
        {
            private IEnemeyDataModel _enemeyDataModel => this.GetModel<IEnemeyDataModel>();

            protected override int OnExecute()
            {
                return _enemeyDataModel.AddEnemyToCache(EnemyEnum.Enemy1);
            }
        }

        public class Damage : AbstractCommand
        {
            private IEnemeyDataModel _enemeyDataModel => this.GetModel<IEnemeyDataModel>();
            
            private int _id;
            private int _damage;
            

            public Damage(int id, int damage)
            {
                _id = id;
                _damage = damage;
            }

            protected override void OnExecute()
            {
                _enemeyDataModel.DamageEnemy(_id, _damage);
            }
        }

    }
}
