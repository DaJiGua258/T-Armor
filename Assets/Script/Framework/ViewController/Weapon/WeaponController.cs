using System.Threading;
using UnityEngine;
using QFramework.Command;
using QFramework.System;
using QFramework.Enum;
using QFramework.Utility;
using Unity.VisualScripting;
using QFramework.Event;
using QFramework.UtilityKit;

namespace QFramework.ViewController.Player
{
    public enum WeaponSlotEnum
    {
        Left,
        Right
    }

    public class WeaponController : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        public AbstractWeapon WeaponLeft;  // 武器脚本
        public AbstractWeapon WeaponRight;
        [SerializeField] private Transform _weaponSlotLeft;
        [SerializeField] private Transform _weaponSlotRight;
        [SerializeField] private Rigidbody2D _targetRig;

        private IPlayerSystem _playerSystem => this.GetSystem<IPlayerSystem>();
        

        void Awake()
        {
            // _weaponSlotLeft = transform.Find("Body/WeaponSlotLeft");
            // _weaponSlotRight = transform.Find("Body/WeaponSlotRight");
            
            WeaponLeft = _weaponSlotLeft.GetComponentInChildren<AbstractWeapon>();
            WeaponRight = _weaponSlotRight.GetComponentInChildren<AbstractWeapon>();
        }

        void Start()
        {
            _playerSystem.PlayerWeapon.Left.Register(OnWeaponLeftDataChanged);
            _playerSystem.PlayerWeapon.Right.Register(OnWeaponRightDataChanged);

            TypeEventSystem.Global.Register<WeaponEvent.GetTargetRig>(e => GetTargetRig(e.TargetRig))
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            this.SendCommand(new WeaponCommand.Init());
        }

        /// <summary>
        /// 旋转武器
        /// </summary>
        public void RotateWeapon(Vector3 hitPos, float aimZOffsetDeg)
        {
            void RotateWeaponDetail(AbstractWeapon weapon, Vector3 hitPos, float aimZOffsetDeg)
            {
                // 计算目标朝向
                Vector3 dir = hitPos - weapon.transform.position;

                // 计算旋转角度
                float targetZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + aimZOffsetDeg;

                // 平滑插值
                float currentZ = weapon.transform.rotation.eulerAngles.z;
                
                // 提前量计算
                if(_targetRig != null)
                {
                    currentZ += MathTool.CalculateLeadAngle2D(
                        weapon.transform.position, 
                        weapon.WeaponDataModel.BulletSpeed, 
                        _targetRig.position, 
                        _targetRig.velocity);
                }
                float smoothZ = Mathf.LerpAngle(currentZ, targetZ, 10f * Time.deltaTime);

                // 设置旋转
                weapon.transform.rotation = Quaternion.Euler(0f, 0f, smoothZ);
            }

            RotateWeaponDetail(WeaponLeft, hitPos, aimZOffsetDeg);
            RotateWeaponDetail(WeaponRight, hitPos, aimZOffsetDeg);
            
            return;
        }

        public void GetTargetRig(Rigidbody2D targetRig)
        {
            _targetRig = targetRig;
        }


        /// <summary>
        /// 左槽位武器数据变化的事件封装
        /// </summary>
        /// <param name="weaponData"></param>
        public void OnWeaponLeftDataChanged(WeaponDataModel weaponData)
        {
            OnWeaponDataChanged(WeaponSlotEnum.Left, weaponData);
        }

        /// <summary>
        /// 右槽位武器数据变化的事件封装
        /// </summary>
        /// <param name="weaponData"></param>
        public void OnWeaponRightDataChanged(WeaponDataModel weaponData)
        {
            OnWeaponDataChanged(WeaponSlotEnum.Right, weaponData);
        }

        /// <summary>
        /// 武器数据变化
        /// </summary>
        /// <param name="weaponData"></param>
        private void OnWeaponDataChanged(WeaponSlotEnum weaponSlotEnum, WeaponDataModel weaponData)
        {
            Transform weaponSlot = weaponSlotEnum == WeaponSlotEnum.Left ? _weaponSlotLeft : _weaponSlotRight;
            if(weaponSlot == null) return;

            AbstractWeapon weapon = null;

            // 如果左槽位有武器，则销毁
            if(weaponSlot.childCount > 0)
            {
                Destroy(weaponSlot.GetChild(0).gameObject);
            }

            weapon = InstantiateWeapons(weaponData.WeaponType, weaponSlot, weaponData);

            if(weaponSlotEnum == WeaponSlotEnum.Left)
            {
                WeaponLeft = weapon;
            }
            else if(weaponSlotEnum == WeaponSlotEnum.Right)
            {
                WeaponRight = weapon;
            }
        }

        /// <summary>
        /// 初始化并生成武器
        /// </summary>
        private AbstractWeapon InstantiateWeapons(WeaponTypeEnum weaponType, Transform weaponSlot, WeaponDataModel weaponData)
        {
            AbstractWeapon weapon = null;
            var rifle = Instantiate(
                this.GetUtility<IResourceLoad>().Load<GameObject>("Prefab/Weapon/Slot/" + weaponType.ToString()), weaponSlot);
            weapon = rifle.GetComponent<AbstractWeapon>();
            weapon.InitWeaponData(weaponData);
            weapon.SetOwner(gameObject);

            return weapon;
        }

    }
}
