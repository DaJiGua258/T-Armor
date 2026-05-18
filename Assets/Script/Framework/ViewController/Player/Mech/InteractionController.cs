using QFramework.Command;
using QFramework.Enum;
using QFramework.Event;
using QFramework.Manager;
using QFramework.System;
using QFramework.Utility;
using QFramework.ViewController.UI;
using UnityEngine;

namespace QFramework.ViewController.Player
{
    public class InteractionController : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        private IObjectPoolUtility _objectPoolUtility => this.GetUtility<IObjectPoolUtility>();
        private IInputUtility _inputUtility => this.GetUtility<IInputUtility>();

        private AimingModeEnum _currentMode;

        private Grabbable _heldGrabbable;
        public static Grabbable HeldGrabbable { get; private set; }

        private Transform _playerBody;

        private void Awake()
        {
            _playerBody = transform.Find("Mesh/Body");
        }

        private void Start()
        {
            TypeEventSystem.Global.Register<PlayerEvent.SwitchAimingMode>(
                e => _currentMode = e.Mode
            ).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        void Update()
        {
            if (_currentMode != AimingModeEnum.Interaction) return;

            // Modal 面板打开时不响应交互（防止与 TerminalPanel 的 F 键关闭冲突）
            if (UIGameManager.Instance.IsModalActive()) return;

            // 跟随逻辑：持有物体时放在玩家 Body 前方
            if (_heldGrabbable != null && _playerBody != null)
            {
                _heldGrabbable.transform.position = _playerBody.position + _playerBody.right * _heldGrabbable.FollowDistance;
            }

            if (!_inputUtility.GetPickUpItemInput()) return;

            var target = AimFrame.CurrentAimTarget;
            if (target == null) return;

            ExecuteInteraction(target);
        }

        private void ExecuteInteraction(GameObject target)
        {
            // 锁定目标不执行交互
            Interactable lockedInter = target.GetComponent<Interactable>();
            if (lockedInter != null && lockedInter.IsLocked) return;
            Grabbable lockedGrab = target.GetComponent<Grabbable>();
            if (lockedGrab != null && lockedGrab.IsLocked) return;

            // 放置检测：持有物体 + 目标有 socket + 类型匹配
            if (_heldGrabbable != null)
            {
                Interactable interactable = target.GetComponent<Interactable>();
                if (interactable != null && interactable.HasSocket
                    && interactable.AcceptedType == _heldGrabbable.ItemType)
                {
                    _heldGrabbable.AttachToSocket(interactable.PlacementSocket);
                    interactable.OnPlaceObject();
                    _heldGrabbable = null;
                    UpdateHeldStatic();
                    return;
                }
            }

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

            IInteractable interactableInterface = target.GetComponent<IInteractable>();
            if (interactableInterface != null)
            {
                interactableInterface.OnInteract(gameObject);

                // 如果是 Grabbable，更新持有引用
                Grabbable grabbable = interactableInterface as Grabbable;
                if (grabbable != null)
                {
                    if (grabbable.IsGrabbed)
                        _heldGrabbable = grabbable;
                    else if (_heldGrabbable == grabbable)
                        _heldGrabbable = null;
                    UpdateHeldStatic();
                }
            }
        }

        private void UpdateHeldStatic()
        {
            HeldGrabbable = _heldGrabbable;
        }

        private void OnPickUpWeapon(PickUpItems pickUp)
        {
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
