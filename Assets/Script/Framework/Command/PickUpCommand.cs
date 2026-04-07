using QFramework.Enum;
using QFramework.System;
using UnityEngine;

namespace QFramework.Command
{
    public class PickUpCommand
    {
        public class AddPickUpWeaponInstance : AbstractCommand<int>
        {
            private IWeaponInstanceSystem _weaponInstanceSystem => this.GetSystem<IWeaponInstanceSystem>();

            private WeaponTypeEnum _weaponType;
            public AddPickUpWeaponInstance(WeaponTypeEnum weaponType)
            {
                this._weaponType = weaponType;
            }

            protected override int OnExecute()
            {
                return _weaponInstanceSystem.AddWeapon(_weaponType);
            }
        }


        public class PickUpWeapon : AbstractCommand
        {
            private IWeaponInstanceSystem _weaponInstanceSystem => this.GetSystem<IWeaponInstanceSystem>();
            private IPlayerSystem _playerSystem => this.GetSystem<IPlayerSystem>();
            private WeaponDataModel _currentWeaponData;
            private int _currentId;  // 当前武器ID
            private WeaponDataModel _targetWeaponData;
            private int _targetId;  // 目标武器ID
            

            /// <summary>
            /// 根据当前输入的武器ID，目标武器ID，交换武器数据
            /// </summary>
            public PickUpWeapon(int currentId, int targetId)
            {
                this._targetId = targetId;
                this._currentId = currentId;
            }

            protected override void OnExecute()
            {
                WeaponDataModel currentWeaponData = null;
                if(_currentId == _playerSystem.PlayerWeapon.WeaponDataLeft.Value.InstanceId)
                {
                    // 先在这里获取到当前持有的武器的引用
                    currentWeaponData = _playerSystem.PlayerWeapon.WeaponDataLeft.Value;
                    
                    // 然后在从缓存中移除目标武器，并将从缓存中获取的目标武器赋值给当前武器
                    _playerSystem.PlayerWeapon.WeaponDataLeft.Value = _weaponInstanceSystem.RemoveWeaponById(_targetId);
                }
                else if(_currentId == _playerSystem.PlayerWeapon.WeaponDataRight.Value.InstanceId)
                {
                    currentWeaponData = _playerSystem.PlayerWeapon.WeaponDataRight.Value;
                    _playerSystem.PlayerWeapon.WeaponDataRight.Value = _weaponInstanceSystem.RemoveWeaponById(_targetId);
                }
                else
                {
                    Debug.LogError("当前武器ID不存在");
                    return;
                }
                
                // 最后将之前持有的武器添加到缓存中，完成交换
                _weaponInstanceSystem.AddExistingWeapon(currentWeaponData);
            }
        }
    }
}
