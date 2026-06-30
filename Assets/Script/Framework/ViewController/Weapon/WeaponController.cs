using System.Threading;
using UnityEngine;
using QFramework.Command;
using QFramework.System;
using QFramework.Enum;
using QFramework.Utility;
using Unity.VisualScripting;
using QFramework.Event;
using QFramework.UtilityKit;
using Pathfinding;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        private FollowerEntity _targetFollower;

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
            _playerSystem.PlayerWeapon.Left.Register(OnWeaponLeftDataChanged)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
            _playerSystem.PlayerWeapon.Right.Register(OnWeaponRightDataChanged)
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            // 如果已有武器数据（从主菜单带入），手动触发槽位武器实例化
            if (_playerSystem.PlayerWeapon.Left.Value != null)
                OnWeaponLeftDataChanged(_playerSystem.PlayerWeapon.Left.Value);
            if (_playerSystem.PlayerWeapon.Right.Value != null)
                OnWeaponRightDataChanged(_playerSystem.PlayerWeapon.Right.Value);

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

                // 提前量计算：加到目标角度上，使武器指向目标的预测位置
                if(_targetRig != null)
                {
                    Vector2 targetVel = _targetFollower != null && _targetFollower.enabled
                        ? _targetFollower.velocity
                        : _targetRig.velocity;

                    targetZ += MathTool.CalculateLeadAngle2D(
                        weapon.transform.position,
                        weapon.WeaponDataModel.BulletSpeed,
                        _targetRig.position,
                        targetVel);
                }

                // 平滑插值
                float currentZ = weapon.transform.rotation.eulerAngles.z;
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
            _targetFollower = targetRig != null ? targetRig.GetComponent<FollowerEntity>() : null;
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

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            DrawWeaponGizmo(WeaponLeft, _weaponSlotLeft);
            DrawWeaponGizmo(WeaponRight, _weaponSlotRight);
        }

        private void DrawWeaponGizmo(AbstractWeapon weapon, Transform weaponSlot)
        {
            if (weapon == null || _targetRig == null || weapon.WeaponDataModel == null) return;

            Vector3 muzzlePos = weapon.Muzzle != null ? weapon.Muzzle.position : weapon.transform.position;
            Vector3 targetPos = _targetRig.position;

            // 1. 当前武器实际指向 (蓝色) — 包含提前量 + 平滑插值后的结果
            Gizmos.color = Color.blue;
            Vector3 currentDir = weapon.transform.right;
            Gizmos.DrawRay(muzzlePos, currentDir * 8f);
            Handles.color = Color.blue;
            Handles.DrawSolidDisc(muzzlePos + currentDir * 8f, Vector3.forward, 0.15f);

            // 2. 未计算提前量的瞄准方向 (红色) — 直接指向目标当前位置
            Gizmos.color = Color.red;
            Vector3 rawDir = ((Vector3)(Vector2)targetPos - muzzlePos).normalized;
            Gizmos.DrawRay(muzzlePos, rawDir * 8f);
            Handles.color = Color.red;
            Handles.DrawSolidDisc(muzzlePos + rawDir * 8f, Vector3.forward, 0.15f);

            // 3. 计算提前量后的瞄准方向 (绿色) — 指向目标的预测位置
            Vector2 targetVel = _targetFollower != null && _targetFollower.enabled
                ? _targetFollower.velocity
                : _targetRig.velocity;

            float leadAngle = MathTool.CalculateLeadAngle2D(
                muzzlePos,
                weapon.WeaponDataModel.BulletSpeed,
                targetPos,
                targetVel);

            float rawAngle = Mathf.Atan2(rawDir.y, rawDir.x) * Mathf.Rad2Deg;
            float leadAngleDeg = rawAngle + leadAngle;
            Vector3 leadDir = Quaternion.Euler(0f, 0f, leadAngleDeg) * Vector3.right;

            Gizmos.color = Color.green;
            Gizmos.DrawRay(muzzlePos, leadDir * 8f);
            Handles.color = Color.green;
            Handles.DrawSolidDisc(muzzlePos + leadDir * 8f, Vector3.forward, 0.15f);

            // 辅助：在目标位置画标签
            Handles.color = Color.yellow;
            Handles.Label(targetPos, "Target");
        }
#endif
    }
}
