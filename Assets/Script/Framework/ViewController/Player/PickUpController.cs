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



        void Update()
        {
            UpdatePickUpCheck();
        }

        private void UpdatePickUpCheck()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
            if(hit.collider != null)
            {
                if(hit.collider.gameObject.CompareTag("PickUp") && Input.GetMouseButtonDown(0))
                {
                    PickUpItems pickUp = hit.collider.gameObject.GetComponent<PickUpItems>();
                    if(pickUp._type == TypeEnum.Weapon)
                    {
                        this.SendCommand(new PickUpCommand.PickUpWeapon(
                            this.GetSystem<IPlayerSystem>().PlayerWeapon.WeaponDataLeft.Value.InstanceId, 
                            pickUp.GetInstanceId()));
                    }

                }   
            }
        }
    }    
}