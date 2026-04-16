using System.Threading;
using UnityEngine;
using QFramework.Command;
using QFramework.System;
using QFramework.Enum;
using QFramework.Utility;
using Unity.VisualScripting;

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
        private Transform _weaponSlotLeft;
        private Transform _weaponSlotRight;

        private IPlayerSystem _playerSystem => this.GetSystem<IPlayerSystem>();
        

        void Awake()
        {
            _weaponSlotLeft = transform.Find("Body/WeaponSlotLeft");
            _weaponSlotRight = transform.Find("Body/WeaponSlotRight");
            
            WeaponLeft = _weaponSlotLeft.GetComponentInChildren<AbstractWeapon>();
            WeaponRight = _weaponSlotRight.GetComponentInChildren<AbstractWeapon>();
        }

        void Start()
        {
            _playerSystem.PlayerWeapon.WeaponDataLeft.Register(OnWeaponLeftDataChanged);
            _playerSystem.PlayerWeapon.WeaponDataRight.Register(OnWeaponRightDataChanged);

            this.SendCommand(new WeaponCommand.Init());
        }

        /// <summary>
        /// 旋转武器
        /// </summary>
        public void RotateWeapon(Vector3 hitPos, Transform body, float aimZOffsetDeg)
        {
            if (!WeaponLeft || !WeaponRight)
            {
                Debug.Log("WeaponLeft or WeaponRight is null");
                return;
            }

            Vector3 hit = hitPos;

            Vector3 dirLeft = hit - WeaponLeft.transform.position;
            Vector3 dirRight = hit - WeaponRight.transform.position;

            // 在这里实现旋转平滑效果
            // 当前Weapon的朝向（欧拉角z)
            float currentZLeft = WeaponLeft.transform.rotation.eulerAngles.z;
            float currentZRight = WeaponRight.transform.rotation.eulerAngles.z;

            // 计算目标朝向
            float targetZLeft = Mathf.Atan2(dirLeft.y, dirLeft.x) * Mathf.Rad2Deg + aimZOffsetDeg;
            float targetZRight = Mathf.Atan2(dirRight.y, dirRight.x) * Mathf.Rad2Deg + aimZOffsetDeg;

            // 在360度环绕下插值
            float smoothZLeft = Mathf.LerpAngle(currentZLeft, targetZLeft, 10f * Time.deltaTime);
            float smoothZRight = Mathf.LerpAngle(currentZRight, targetZRight, 10f * Time.deltaTime);

            WeaponLeft.transform.rotation = Quaternion.Euler(0f, 0f, smoothZLeft);
            WeaponRight.transform.rotation = Quaternion.Euler(0f, 0f, smoothZRight);

            return;
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
                Destroy(weaponSlot.GetChild(0).gameObject);

            // 如果武器数据为 Rifle，则生成 Rifle 武器
            if(weaponData.WeaponType == WeaponTypeEnum.AR)
            {
                weapon = InstantiateWeapons(WeaponTypeEnum.AR, weaponSlot, weaponData);
            }
            else if(weaponData.WeaponType == WeaponTypeEnum.MG)
            {
                weapon = InstantiateWeapons(WeaponTypeEnum.MG, weaponSlot, weaponData);
            }
            else if(weaponData.WeaponType == WeaponTypeEnum.SG)
            {
                weapon = InstantiateWeapons(WeaponTypeEnum.SG, weaponSlot, weaponData);
            }

        
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
                this.GetUtility<IResourceLoad>().Load<GameObject>("Prefab/Weapon/" + weaponType.ToString()), weaponSlot);
            weapon = rifle.GetComponent<AbstractWeapon>();
            weapon.InitWeaponData(weaponData);

            return weapon;
        }

    }
}
