using QFramework.Enum;
using QFramework.System;
using UnityEngine;

namespace QFramework.Command
{
    public class PickUpCommand
    {
        public class AddPickUpItemInstance : AbstractCommand<int>
        {
            private IPickUpItemInstanceSystem _pickUpItemInstanceSystem => this.GetSystem<IPickUpItemInstanceSystem>();
            private ItemTypeEnum _itemType;
            public AddPickUpItemInstance(ItemTypeEnum itemType)
            {
                this._itemType = itemType;
            }

            protected override int OnExecute()
            {
                return _pickUpItemInstanceSystem.AddItemInstance(_itemType);
            }
        }

        /// <summary>
        /// 添加拾取物品实例
        /// </summary>
        public class PickUpItemInstance : AbstractCommand
        {
            private IPickUpItemInstanceSystem _pickUpItemInstanceSystem => this.GetSystem<IPickUpItemInstanceSystem>();
            private IInvenotrySystem _invenotrySystem => this.GetSystem<IInvenotrySystem>();
            private int itemInstanceId;
            public PickUpItemInstance(int itemInstanceId)
            {
                this.itemInstanceId = itemInstanceId;
            }

            protected override void OnExecute()
            {
                int nullSlotIndex = -1;
                var pickUpItemData = _pickUpItemInstanceSystem.ItemDataInstanceCache[itemInstanceId];  // 从实例缓存字典获取物品的数据
                int overflow = 0;  // 溢出数量

                // 遍历背包槽位
                for(int i = 0; i < _invenotrySystem.ItemDataCache.Count; i++)
                {
                    var slotData = _invenotrySystem.ItemDataCache[i];  // 获取的背包槽位数据
                    if(slotData.ItemType == ItemTypeEnum.None && nullSlotIndex == -1)
                    {
                        nullSlotIndex = i;
                    }
                    // 如果背包槽位物品类型与拾取的物品类型相同
                    if(slotData.ItemType == pickUpItemData.ItemType)
                    {
                        // 如果背包槽位物品可以堆叠，并且拾取的物品数量与背包槽位物品数量之和不超过背包槽位物品的最大数量
                        if(slotData.canStack && slotData.Count.Value + pickUpItemData.Count.Value <= slotData.maxStack)
                        {
                            slotData.Count.Value += pickUpItemData.Count.Value;
                            return;
                        }
                        // 如果背包槽位物品可以堆叠，并且拾取的物品数量与背包槽位物品数量之和超过背包槽位物品的最大数量
                        else if(slotData.canStack && slotData.Count.Value + pickUpItemData.Count.Value > slotData.maxStack)
                        {
                            // 计算溢出数量(背包槽位物品数量 + 拾取的物品数量 - 背包槽位物品的最大数量)
                            overflow = slotData.Count.Value + (pickUpItemData.Count.Value - overflow) - slotData.maxStack;
                            slotData.Count.Value = slotData.maxStack;
                        }
                    }
                    
                }

                if(nullSlotIndex != -1)
                {
                    _invenotrySystem.AddItem(nullSlotIndex, pickUpItemData);
                    return;
                }
                else if(overflow > 0)
                {
                    _invenotrySystem.SetItemCount(nullSlotIndex, overflow);
                    return;
                }

                // 将拾取完成的物品从字典缓存中删去
                _pickUpItemInstanceSystem.RemoveItemInstance(itemInstanceId);
            }
        }
            

            /// <summary>
            /// 添加拾取武器实例
            /// </summary>
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
