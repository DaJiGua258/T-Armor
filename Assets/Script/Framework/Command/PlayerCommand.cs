using QFramework.Model;
using UnityEngine;

namespace QFramework.Command
{
    public class PlayerCommand
    {
        public class Damage : AbstractCommand
        {
            private IPlayerModel _playerModel => this.GetModel<IPlayerModel>();
            private int _damage;

            public Damage(int damage)
            {
                _damage = damage;
            }

            protected override void OnExecute()
            {
                
                _playerModel.CurrentHealth.Value -= _damage;
            }
        }

        

        public class Heal : AbstractCommand
        {
            private IPlayerModel _playerModel => this.GetModel<IPlayerModel>();

            protected override void OnExecute()
            {
                _playerModel.CurrentHealth.Value += 10;
            }
        }


    }
}