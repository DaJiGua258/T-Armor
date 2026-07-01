using UnityEngine;
using QFramework.Utility;
using QFramework.Enum;
using QFramework.System;
using QFramework.Command;
using QFramework.Model;
using QFramework.Event;
using QFramework.Manager;
using QFramework.ViewController.Misc;
using QFramework.ViewController.Player;

namespace QFramework.ViewController.Player
{
    public class AbstractWeapon : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        
        [Header("武器类型")]
        [SerializeField] private WeaponTypeEnum _weaponType;
        [SerializeField] private int _weaponId;

        [Header("武器组件")]
        [SerializeField] private Transform Mesh;
        [SerializeField] public Transform Muzzle;        
        [SerializeField] private Transform _case;
        [SerializeField] protected GameObject _pf_bullet;
        [SerializeField] protected ParticleSystem _vfxShooting;

        [Header("武器属性")]
        public WeaponDataModel WeaponDataModel;

        private GameObject _ownerRef;

        public void SetOwner(GameObject owner) => _ownerRef = owner;

        private float _timer;
        protected LayerMask _bulletLayerMask;

        // 瞄准目标追踪（VML 同款模式，供追踪 Mod 使用）
        private Vector3 _aimTargetPos;
        private Transform _targetTransform;
        private bool _hasReceivedTarget;

        void Awake()
        {
            Mesh = transform.Find("Mesh");
            Muzzle = Mesh.Find("Muzzle");
            _case = Mesh.Find("Case");
            _vfxShooting = Mesh.Find("vfx_shooting").GetComponent<ParticleSystem>();
            _vfxShooting.Stop();
        }

        void Start()
        {
            TypeEventSystem.Global.Register<WeaponEvent.UpdateBulletLayerMask>(
                e => _bulletLayerMask = e.LayerMask
            ).UnRegisterWhenGameObjectDestroyed(gameObject);

            // 监听 AimFrame 的瞄准目标信息（供追踪 Mod 使用）
            TypeEventSystem.Global.Register<PlayerEvent.UpdateTarget>(e =>
            {
                _aimTargetPos = e.Target;
                _hasReceivedTarget = true;
                if (!e.HasTarget) _targetTransform = null;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            TypeEventSystem.Global.Register<WeaponEvent.GetTargetCollider>(e =>
            {
                _targetTransform = e.TargetCollider != null ? e.TargetCollider.transform : null;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        void Update()
        {
            _timer += Time.deltaTime;

            ReloadAuto();
        }

        public void InitWeaponData(WeaponDataModel weaponDataModel)
        {
            WeaponDataModel = weaponDataModel;
            _weaponId = WeaponDataModel.InstanceId.Value;
        }

/// <summary>
/// 生成导弹并初始化其属性
/// </summary>
/// <param name="pos">导弹生成位置</param>
/// <param name="launchIndex">发射索引，用于确定使用哪个发射特效</param>
        public void ReloadByInput()
        {
    // 如果尚未接收到目标位置，则获取鼠标位置作为目标
            if(WeaponDataModel.WeaponState == WeaponStateEnum.Idle
                && WeaponDataModel.CurMagazine.Value < WeaponDataModel.MaxMagazine
                && WeaponDataModel.CurMaxAmmo.Value > 0)
            {
                this.SendCommand(new WeaponCommand.Reload(WeaponDataModel));
            }
        }

        public void ReloadAuto()
        { 
            if(WeaponDataModel.WeaponState == WeaponStateEnum.Idle
                && WeaponDataModel.CurMagazine.Value < WeaponDataModel.MaxMagazine
                && WeaponDataModel.CurMagazine.Value <= 0
                && WeaponDataModel.CurMaxAmmo.Value > 0)
            {
                this.SendCommand(new WeaponCommand.Reload(WeaponDataModel));
            }
        }


        public void Shoot()
        {
            if(WeaponDataModel == null)
            {
                Debug.LogWarning("WeaponDataModel is null");
                return;
            }

            // 开火间隔 & 弹匣有弹药
            if(_timer < 60f / WeaponDataModel.Rpm  // 60秒 / 每分钟子弹数 = 开火间隔
                ||  WeaponDataModel.WeaponState == WeaponStateEnum.Reloading
                || WeaponDataModel.CurMagazine.Value <= 0)
                return;


            if (_pf_bullet == null || Muzzle == null)
            {
                Debug.LogError("BulletPrefab or BulletSpawnPoint or Muzzle is null");
                return;
            }

            ShootDetal();
            AudioManager.Instance.PlaySFX(ShootSFXType);
            this.SendCommand(WeaponCommand.Shoot.Instance.Init(WeaponDataModel));
            _timer = 0;
        }

        public virtual SFXType ShootSFXType => SFXType.weapon_shoot_ar;

        public virtual void AimAt(Vector3 hitPos, float offset) { }

        public virtual void ShootDetal()
        {
            // 计算发射方向：与武器朝向一致
            Vector3 shootDir = Muzzle.right; // local right 是2D武器的默认枪口方向 (一般为右)

            // 应用散射角度
            float spread = WeaponDataModel.SpreadAngle;
            if (spread > 0f)
            {
                float randomAngle = Random.Range(-spread * 0.5f, spread * 0.5f);
                shootDir = Quaternion.Euler(0, 0, randomAngle) * shootDir;
            }

            SpawnBullet(shootDir);
        }

        /// <summary>
        /// 生成子弹的工具方法
        /// </summary>
        protected void SpawnBullet(Vector3 dir)
        {
            GameObject bullet = this.GetUtility<IObjectPoolUtility>().GetObject(_pf_bullet, Muzzle.position, Muzzle.rotation);
            _vfxShooting.Play();

            Projectile bulletComponent = bullet.GetComponent<Projectile>();

            // 计算有效穿透次数：整数部分=必定穿透，小数部分=额外穿透概率
            float penValue = WeaponDataModel.Penetration;
            int guaranteed = Mathf.FloorToInt(penValue);
            int effectivePen = guaranteed + (UnityEngine.Random.value < penValue - guaranteed ? 1 : 0);

            var damageInfo = new DamageInfo(WeaponDataModel.BulletDamage, WeaponDataModel.KnockbackValue, WeaponDataModel.BurnValue, dir, WeaponDataModel.SlowValue, effectivePen);
            bulletComponent.InitBullet(dir, WeaponDataModel.BulletSpeed, damageInfo, _ownerRef);
            bulletComponent.SetLayerMask(_bulletLayerMask);

            // 子弹追踪：仅在锁定敌人时追踪，无目标则直线飞行
            if (WeaponDataModel.EnableHoming && _targetTransform != null)
            {
                bulletComponent.SetHomingTarget(_targetTransform);

                // 飞行 2 单位后才开始追踪（VML 走垂直发射不由这里控制）
                bulletComponent.SetHomingDelayDistance(2f);
            }
        }
    }
}

