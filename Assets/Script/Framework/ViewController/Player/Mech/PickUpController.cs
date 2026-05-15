using QFramework.Command;
using QFramework.Enum;
using QFramework.Event;
using QFramework.System;
using QFramework.Utility;
using QFramework.ViewController.Player;
using UnityEngine;

namespace QFramework.ViewController.Player
{
    public class PickUpController : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        private IObjectPoolUtility _objectPoolUtility => this.GetUtility<IObjectPoolUtility>();
        private IInputUtility _inputUtility => this.GetUtility<IInputUtility>();

        [SerializeField] private float _interactRange = 3f;

        private GameObject _currentTarget;
        private bool _hasPromptShown;

        void Update()
        {
            UpdatePickUpCheck();
        }

        private void UpdatePickUpCheck()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, _interactRange);

            if (hit.collider != null && hit.collider.CompareTag("AimTarget"))
            {
                _currentTarget = hit.collider.gameObject;
                ShowPrompt(_currentTarget);

                if (_inputUtility.GetPickUpItemInput())
                {
                    ExecuteInteraction(_currentTarget);
                }
            }
            else
            {
                if (_hasPromptShown)
                {
                    TypeEventSystem.Global.Send(new InteractionEvent.HidePrompt());
                    _hasPromptShown = false;
                }
                _currentTarget = null;
            }
        }

        private void ShowPrompt(GameObject target)
        {
            string actionText = "";
            string nameText = "";

            PickUpItems pickUp = target.GetComponent<PickUpItems>();
            if (pickUp != null)
            {
                actionText = "拾取";
                nameText = GetPickUpDisplayName(pickUp);
            }
            else
            {
                IInteractable interactable = target.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    actionText = interactable.InteractionText;
                    nameText = interactable.DisplayName;
                }
            }

            if (!string.IsNullOrEmpty(actionText))
            {
                TypeEventSystem.Global.Send(new InteractionEvent.ShowPrompt
                {
                    ActionText = actionText,
                    NameText = nameText
                });
                _hasPromptShown = true;
            }
        }

        private string GetPickUpDisplayName(PickUpItems pickUp)
        {
            switch (pickUp._type)
            {
                case TypeEnum.Weapon:
                    return pickUp._weaponType.ToString();
                case TypeEnum.Item:
                    return this.GetModel<QFramework.Model.IItemConfigModel>()
                        .GetItemConfig(pickUp._pickUpType).name;
                default:
                    return "未知";
            }
        }

        private void ExecuteInteraction(GameObject target)
        {
            PickUpItems pickUp = target.GetComponent<PickUpItems>();
            if (pickUp != null)
            {
                switch (pickUp._type)
                {
                    case TypeEnum.Weapon:
                        OnPickUpWeapon(pickUp);
                        break;
                    case TypeEnum.Item:
                        OnPickUpItem(pickUp);
                        break;
                }
                return;
            }

            IInteractable interactable = target.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.OnInteract(gameObject);
            }
        }

        private void OnPickUpWeapon(PickUpItems pickUp)
        {
            int currentWeaponId = this.GetSystem<IPlayerSystem>().PlayerWeapon.Left.Value.InstanceId.Value;

            this.SendCommand(new PickUpCommand.PickUpWeapon(
                this.GetSystem<IPlayerSystem>().PlayerWeapon.Left.Value.InstanceId.Value,
                pickUp.GetInstanceId()));

            _objectPoolUtility.PushObject(pickUp.gameObject);
        }

        private void OnPickUpItem(PickUpItems pickUp)
        {
            this.SendCommand(new PickUpCommand.PickUpItemInstance(
                pickUp.GetInstanceId()));

            _objectPoolUtility.PushObject(pickUp.gameObject);
        }
    }
}
