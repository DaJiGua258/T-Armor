using QFramework.Model;
using UnityEngine;

namespace QFramework.Command
{
    public class PlayerCommand
    {
        public class Damage : AbstractCommand
        {
            private IPlayerModel _playerModel => this.GetModel<IPlayerModel>();

            public static Damage Instance = new();
            private int _damage;
            private Damage() { }
            

            public Damage Init(int damage)
            {
                _damage = damage;
                return this;
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

        public class ConsumeFuel : AbstractCommand
        {
            private IPlayerModel _playerModel => this.GetModel<IPlayerModel>();

            public static ConsumeFuel Instance = new();
            private float _fuel;

            public ConsumeFuel Init(float fuel)
            {
                _fuel = fuel;
                return this;
            }

            protected override void OnExecute()
            {
                _playerModel.CurrentFuel.Value -= _fuel;
                
                if(_playerModel.CurrentFuel.Value < 0)
                {
                    _playerModel.CurrentFuel.Value = 0;
                }

            }
        }

        public class AddFuel : AbstractCommand
        {
            private IPlayerModel _playerModel => this.GetModel<IPlayerModel>();

            public static AddFuel Instance = new();
            private float _fuel;

            public AddFuel Init(float fuel)
            {
                _fuel = fuel;
                return this;
            }
            protected override void OnExecute()
            {
                _playerModel.CurrentFuel.Value += _fuel;

                if (_playerModel.CurrentFuel.Value > _playerModel.MaxFuel.Value)
                {
                    _playerModel.CurrentFuel.Value = _playerModel.MaxFuel.Value;
                }
            }
        }
    }
}