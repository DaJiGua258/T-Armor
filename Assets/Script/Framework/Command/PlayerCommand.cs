using QFramework.Model;
using QFramework.System;
using QFramework.Enum;
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

        public class UseHotbarItem : AbstractCommand
        {
            public static UseHotbarItem Instance = new();
            private int _index;

            public UseHotbarItem Init(int index)
            {
                _index = index;
                return this;
            }

            protected override void OnExecute()
            {
                var invSystem = this.GetSystem<IInvenotrySystem>();
                var item = invSystem.GetHotbarItemByIndex(_index);

                if (item == null || item.ItemType == ItemTypeEnum.None || !item.canUse)
                    return;

                item.Count.Value--;
                if (item.Count.Value <= 0)
                {
                    // 重置物品状态为 None
                    item.ItemType = ItemTypeEnum.None;
                    item.name = "";
                    item.iconPath = "";
                    item.description = "";
                    item.canUse = false;
                    item.Count.Value = 0;

                    // 触发 InstanceId 事件，强制 UI 刷新（Count 事件在 ItemType 重置前已触发）
                    item.InstanceId.Value = -1;
                }
            }
        }
    }
}