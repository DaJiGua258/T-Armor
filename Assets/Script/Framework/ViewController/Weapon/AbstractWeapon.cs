using UnityEngine;
using QFramework.Utility;
using QFramework.Enum;
using QFramework.System;
using QFramework.Command;
using QFramework.Model;

namespace QFramework.ViewController.Player
{
    public class AbstractWeapon : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        
        [Header("武器类型")]
        [SerializeField] private WeaponTypeEnum _weaponType;
        [SerializeField] private int _weaponId;

        [Header("武器组件")]
        [SerializeField] public Transform Muzzle;
        [SerializeField] private Transform _caseSpawnPoint;
        [SerializeField] protected GameObject _pf_bullet;
        [SerializeField] protected ParticleSystem _vfxShooting;

        [Header("武器属性")]
        public WeaponDataModel WeaponDataModel;

        



        private float _timer;

        void Awake()
        {
            Muzzle = transform.Find("Muzzle");
            _caseSpawnPoint = transform.Find("CaseSpawnPoint");
            _vfxShooting = transform.Find("VFX_Shooting").GetComponent<ParticleSystem>();
        }

        void Start()
        {
            
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

        public void ReloadByInput()
        {
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

            _timer = 0;
        }

        public virtual void ShootDetal()
        {
            this.SendCommand(WeaponCommand.Shoot.Instance.Init(WeaponDataModel));
            // 计算发射方向：与武器朝向一致
            Vector3 shootDir = Muzzle.right; // local right 是2D武器的默认枪口方向 (一般为右)
            // 枪口世界坐标

            // 创建子弹和射击特效
            GameObject bullet = this.GetUtility<IObjectPoolUtility>().GetObject(_pf_bullet, Muzzle.position, Muzzle.rotation);
            _vfxShooting.Play();

            Bullet bulletComponent = bullet.GetComponent<Bullet>();
            bulletComponent.InitBullet(shootDir, WeaponDataModel.BulletSpeed, WeaponDataModel.BulletDamage);
        }
    }
}

