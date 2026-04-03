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
                    currentWeaponData = _playerSystem.PlayerWeapon.WeaponDataLeft.Value;
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
                
                _weaponInstanceSystem.AddExistingWeapon(currentWeaponData);
            }
        }
        

        
    }
}
