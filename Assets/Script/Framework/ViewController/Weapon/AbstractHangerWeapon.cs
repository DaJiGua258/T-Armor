using UnityEngine;
using QFramework.Utility;
using QFramework.Enum;
using QFramework.Model;
using QFramework.System;
using QFramework.Command;
using QFramework.Event;
using QFramework.Manager;
using QFramework.ViewController.Player;

namespace QFramework.ViewController.Player
{
    public abstract class AbstractHangerWeapon : MonoBehaviour, IController
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
        private float _timer;
        protected LayerMask _bulletLayerMask;

        public bool IsActive { get; protected set; }

        public void SetOwner(GameObject owner) => _ownerRef = owner;

        protected virtual void Awake()
        {
            Mesh = transform.Find("Mesh");
            Muzzle = Mesh.Find("Muzzle");
            _case = Mesh.Find("Case");
            _vfxShooting = Mesh.Find("VFX_Shooting").GetComponent<ParticleSystem>();
            _vfxShooting.Stop();
        }

        protected virtual void Start()
        {
            TypeEventSystem.Global.Register<WeaponEvent.UpdateBulletLayerMask>(
                e => _bulletLayerMask = e.LayerMask
            ).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        void Update()
        {
            _timer += Time.deltaTime;
            UpdateActive();
            ReloadAuto();
        }

        public void InitWeaponData(WeaponDataModel weaponDataModel)
        {
            WeaponDataModel = weaponDataModel;
            _weaponId = WeaponDataModel.InstanceId.Value;
        }

        public void Trigger()
        {
            OnTrigger();
        }

        protected void ConsumeShot()
        {
            if (WeaponDataModel == null) return;
            this.SendCommand(WeaponCommand.Shoot.Instance.Init(WeaponDataModel));
            _timer = 0;
        }

        protected bool CanFire()
        {
            if (WeaponDataModel == null)
                return false;

            return _timer >= 60f / WeaponDataModel.Rpm
                && WeaponDataModel.WeaponState != WeaponStateEnum.Reloading
                && WeaponDataModel.CurMagazine.Value > 0;
        }

        protected abstract void OnTrigger();

        protected virtual void UpdateActive() { }

        public void ReloadByInput()
        {
            if (WeaponDataModel.WeaponState == WeaponStateEnum.Idle
                && WeaponDataModel.CurMagazine.Value < WeaponDataModel.MaxMagazine
                && WeaponDataModel.CurMaxAmmo.Value > 0)
            {
                this.SendCommand(new WeaponCommand.Reload(WeaponDataModel));
            }
        }

        public void ReloadAuto()
        {
            if (WeaponDataModel.WeaponState == WeaponStateEnum.Idle
                && WeaponDataModel.CurMagazine.Value < WeaponDataModel.MaxMagazine
                && WeaponDataModel.CurMagazine.Value <= 0
                && WeaponDataModel.CurMaxAmmo.Value > 0)
            {
                this.SendCommand(new WeaponCommand.Reload(WeaponDataModel));
            }
        }

        public virtual SFXType ShootSFXType => SFXType.weapon_shoot_ar;

        protected void SpawnBullet(Vector3 dir, Vector3 pos, Quaternion rot)
        {
            GameObject bullet = this.GetUtility<IObjectPoolUtility>().GetObject(_pf_bullet, pos, rot);
            _vfxShooting.Play();
            AudioManager.Instance.PlaySFX(ShootSFXType);

            Projectile bulletComponent = bullet.GetComponent<Projectile>();
            bulletComponent.InitBullet(dir, WeaponDataModel.BulletSpeed, WeaponDataModel.BulletDamage, _ownerRef);
            bulletComponent.SetLayerMask(_bulletLayerMask);
        }
    }
}
