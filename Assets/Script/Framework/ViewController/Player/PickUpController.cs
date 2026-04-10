using QFramework.Command;
using QFramework.Enum;
using QFramework.System;
using QFramework.Utility;
using UnityEngine;

namespace QFramework.ViewController.Player
{
    public class PickUpController : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        private IObjectPoolUtility _objectPoolUtility => this.GetUtility<IObjectPoolUtility>();
        private IInputUtility _inputUtility => this.GetUtility<IInputUtility>();

        void Update()
        {
            UpdatePickUpCheck();
        }


        private void UpdatePickUpCheck()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
            if (hit.collider != null && hit.collider.gameObject.CompareTag("PickUp"))
            {
                if (_inputUtility.GetPickUpItemInput())
                {
                    PickUpItems pickUp = hit.collider.gameObject.GetComponent<PickUpItems>();
                    switch (pickUp._type)
                    {
                        case TypeEnum.Weapon:
                            OnPickUpWeapon(pickUp);
                            break;

                        case TypeEnum.Item:
                            OnPickUpItem(pickUp);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void OnPickUpWeapon(PickUpItems pickUp)
        {
            int currentWeaponId = this.GetSystem<IPlayerSystem>().PlayerWeapon.WeaponDataLeft.Value.InstanceId.Value;

            // 交换数据
            this.SendCommand(new PickUpCommand.PickUpWeapon(
                this.GetSystem<IPlayerSystem>().PlayerWeapon.WeaponDataLeft.Value.InstanceId.Value,
                pickUp.GetInstanceId()));
            
            // TODO: 交换武器
            _objectPoolUtility.PushObject(pickUp.gameObject);
        }

        private void OnPickUpItem(PickUpItems pickUp)
        {
            this.SendCommand(new PickUpCommand.PickUpItemInstance(
                pickUp.GetInstanceId()));
                
            // 销毁
            // TODO: 销毁物品
            _objectPoolUtility.PushObject(pickUp.gameObject);
        }
    }
}