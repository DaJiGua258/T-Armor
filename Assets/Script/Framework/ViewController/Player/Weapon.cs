using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework.Utility;

namespace QFramework.ViewController.Player
{
    public class Weapon : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        [SerializeField] public Transform Muzzle;
        [SerializeField] private Transform _caseSpawnPoint;

        [SerializeField] private int _bulletSpeed;
        [SerializeField] private int _bulletDamage;
        [SerializeField] private float _shootingInterval;
        [SerializeField] GameObject _pf_bullet;
        [SerializeField] GameObject _pf_shootingVFX;

        private float _timer;

        void Awake()
        {
            Muzzle = transform.Find("Muzzle");
            _caseSpawnPoint = transform.Find("CaseSpawnPoint");
        }

        void Update()
        {
            _timer += Time.deltaTime;
        }

        public void Shoot()
        {
            if(_timer < _shootingInterval)
                return;


            if (_pf_bullet == null || Muzzle == null)
            {
                Debug.LogError("BulletPrefab or BulletSpawnPoint or Muzzle is null");
                return;
            }

            // 计算发射方向：与武器朝向一致
            Vector3 shootDir = Muzzle.right; // local right 是2D武器的默认枪口方向 (一般为右)
            // 枪口世界坐标

            // TODO: 对象池化
            // 创建子弹  
            GameObject bullet = this.GetUtility<IObjectPoolUtility>().GetObject(_pf_bullet, Muzzle.position, Muzzle.rotation, null);

            GameObject shootingVFX = this.GetUtility<IObjectPoolUtility>().GetObject(_pf_shootingVFX, Muzzle.position, Muzzle.rotation, null);
            
            //TODO: 

            Destroy(shootingVFX, 1f);
            
            Bullet bulletComponent = bullet.GetComponent<Bullet>();
            bulletComponent.InitBullet(shootDir, _bulletSpeed);

            _timer = 0;
        }
    }
}

