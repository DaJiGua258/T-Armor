using System;
using QFramework.System;
using QFramework.ViewController.UI;
using UnityEngine;

namespace QFramework.Command
{
    public class UICommand
    {
        public class ExchangeSlot : AbstractCommand
        {
            private IInvenotrySystem _invenotrySystem => this.GetSystem<IInvenotrySystem>();
            private int _currentIndex;
            private SlotType _currentSlotType;  
            private int _targetIndex;
            private SlotType _targetSlotType;

            public ExchangeSlot(int currentIndex, SlotType currentSlotType, int targetIndex, SlotType targetSlotType)
            {
                this._currentIndex = currentIndex;
                this._currentSlotType = currentSlotType;
                this._targetIndex = targetIndex;
                this._targetSlotType = targetSlotType;
            }

            protected override void OnExecute()
            {
                var currentItem = GetItemByType(_currentSlotType, _currentIndex);
                var targetItem = GetItemByType(_targetSlotType, _targetIndex);

                currentItem.SwapWith(targetItem);
            }

            private ItemDataModel GetItemByType(SlotType slotType, int index)
            {
                if(slotType == SlotType.Bag)
                {
                    return _invenotrySystem.GetInventoryItemByIndex(index);
                }
                else
                {
                    return null;
                }
            }
        }
    }
}