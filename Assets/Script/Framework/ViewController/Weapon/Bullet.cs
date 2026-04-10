using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;
using QFramework.Utility;
using QFramework.ViewController.Enemy;

namespace QFramework.ViewController.Player
{
    public class Bullet : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private Transform _bulletMesh;
        [SerializeField] private GameObject _pf_bulletExplosionVFX;

        public void InitBullet(Vector3 direction, int bulletSpeed)
        {
            // 初始化子弹刚体
            if(_rb == null)
            {
                _rb = GetComponent<Rigidbody2D>();
                _rb.velocity = direction.normalized * bulletSpeed;
            }
            else
            {
                _rb.velocity = direction.normalized * bulletSpeed;
            }

            // 激活子弹模型
            if(_bulletMesh == null)
            {
                _bulletMesh = transform.Find("BulletMesh");
            }

            _bulletMesh.gameObject.SetActive(true);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            // 根据碰撞对象的标签，进行不同的处理
            if(other.gameObject.CompareTag("Env"))
            {
                BulletExplosion();
            }
            else if (other.gameObject.CompareTag("Enemy"))
            {
                // other.gameObject.GetComponent<EnemyController>().DamageEnemy(10);
                BulletExplosion();
            }

            
        }

        private void BulletExplosion()
        {
            // 停止子弹运动，并隐藏子弹模型
            _rb.velocity = Vector2.zero;
            _bulletMesh.gameObject.SetActive(false);

            GameObject explosionVFX = this.GetUtility<IObjectPoolUtility>()
                        .GetObject(_pf_bulletExplosionVFX, transform.position, transform.rotation);

                    // 添加一次性定时器, 1秒后将子弹爆炸特效推入对象池
                    this.GetUtility<ITimerUtility>().AddOnce(
                        () => this.GetUtility<IObjectPoolUtility>().PushObject(explosionVFX),
                        1f,
                        () => this.GetUtility<IObjectPoolUtility>().PushObject(gameObject)
                    );
        }
    }
}