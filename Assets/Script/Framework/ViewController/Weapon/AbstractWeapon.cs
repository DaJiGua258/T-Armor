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
        protected WeaponDataModel _weaponDataModel;

        



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
            _weaponDataModel = weaponDataModel;
            _weaponId = _weaponDataModel.InstanceId.Value;
        }

        public void ReloadByInput()
        {
            if(_weaponDataModel.WeaponState == WeaponStateEnum.Idle
                && _weaponDataModel.CurMagazine.Value < _weaponDataModel.MaxMagazine
                && _weaponDataModel.CurMagazine.Value > 0
                && _weaponDataModel.CurMaxAmmo.Value > 0)
            {
                this.SendCommand(new WeaponCommand.Reload(_weaponDataModel));
            }
        }

        public void ReloadAuto()
        { 
            if(_weaponDataModel.WeaponState == WeaponStateEnum.Idle
                && _weaponDataModel.CurMagazine.Value < _weaponDataModel.MaxMagazine
                && _weaponDataModel.CurMagazine.Value == 0
                && _weaponDataModel.CurMaxAmmo.Value > 0)
            {
                this.SendCommand(new WeaponCommand.Reload(_weaponDataModel));
            }
        }


        public void Shoot()
        {
            if(_weaponDataModel == null)
            {
                Debug.LogWarning("WeaponDataModel is null");
                return;
            }

            // 开火间隔 & 弹匣有弹药
            if(_timer < 60f / _weaponDataModel.Rpm  // 60秒 / 每分钟子弹数 = 开火间隔
                ||  _weaponDataModel.WeaponState == WeaponStateEnum.Reloading)
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
            this.SendCommand(WeaponCommand.Shoot.Instance.Init(_weaponDataModel));
            // 计算发射方向：与武器朝向一致
            Vector3 shootDir = Muzzle.right; // local right 是2D武器的默认枪口方向 (一般为右)
            // 枪口世界坐标

            // 创建子弹和射击特效
            GameObject bullet = this.GetUtility<IObjectPoolUtility>().GetObject(_pf_bullet, Muzzle.position, Muzzle.rotation);
            _vfxShooting.Play();

            Bullet bulletComponent = bullet.GetComponent<Bullet>();
            bulletComponent.InitBullet(shootDir, _weaponDataModel.BulletSpeed, _weaponDataModel.BulletDamage);
        }
    }
}

