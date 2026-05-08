using UnityEngine;
using QFramework.Command;
using QFramework.System;
using QFramework.Enum;
using QFramework.Model;
using QFramework.Utility;

namespace QFramework.ViewController.Player
{
    public class HangerWeaponController : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        public AbstractHangerWeapon WeaponLeft;
        public AbstractHangerWeapon WeaponRight;

        [SerializeField] private Transform _slotLeft;
        [SerializeField] private Transform _slotRight;

        private IPlayerSystem _playerSystem => this.GetSystem<IPlayerSystem>();

        void Awake()
        {
            if (_slotLeft != null)
                WeaponLeft = _slotLeft.GetComponentInChildren<AbstractHangerWeapon>();
            if (_slotRight != null)
                WeaponRight = _slotRight.GetComponentInChildren<AbstractHangerWeapon>();
        }

        void Start()
        {
            _playerSystem.PlayerWeapon.HangerLeft.Register(OnDataLeftChanged);
            _playerSystem.PlayerWeapon.HangerRight.Register(OnDataRightChanged);

            this.SendCommand(new WeaponCommand.InitHanger());
        }

        private void OnDataLeftChanged(WeaponDataModel data)
        {
            WeaponLeft = ReplaceWeapon(_slotLeft, data);
        }

        private void OnDataRightChanged(WeaponDataModel data)
        {
            WeaponRight = ReplaceWeapon(_slotRight, data);
        }

        private AbstractHangerWeapon ReplaceWeapon(Transform slot, WeaponDataModel data)
        {
            if (slot == null) return null;

            if (slot.childCount > 0)
            {
                Destroy(slot.GetChild(0).gameObject);
            }

            var prefab = this.GetUtility<IResourceLoad>().Load<GameObject>("Prefab/Weapon/Hanger/" + data.WeaponType.ToString());
            var weapon = Instantiate(prefab, slot).GetComponent<AbstractHangerWeapon>();
            weapon.InitWeaponData(data);
            weapon.SetOwner(gameObject);
            return weapon;
        }
    }
}
