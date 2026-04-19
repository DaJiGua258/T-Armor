using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;
using QFramework.Utility;
using QFramework.ViewController.Enemy;
using Unity.VisualScripting;

namespace QFramework.ViewController.Player
{
    public class Bullet : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        [SerializeField] private LayerMask _layerMask;
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private Transform _bulletMesh;
        [SerializeField] private GameObject _pf_bulletExplosionVFX;
        private bool _hasExploded;

        void FixedUpdate()
        {
            DetectByRay();
        }

        public void InitBullet(Vector3 direction, int bulletSpeed)
        {
            // 初始化子弹刚体
            if(_rb == null)
            {
                _rb = GetComponent<Rigidbody2D>();
            }
            _rb.velocity = direction.normalized * bulletSpeed;

            _hasExploded = false;

            // 激活子弹模型
            if(_bulletMesh == null)
            {
                _bulletMesh = transform.Find("BulletMesh");
            }

            _bulletMesh.gameObject.SetActive(true);
        }

        // void OnTriggerEnter2D(Collider2D other)
        // {
        //     // 根据碰撞对象的标签，进行不同的处理
        //     if(other.gameObject.CompareTag("Env"))
        //     {
        //         BulletExplosion();
        //     }
        //     else if (other.gameObject.CompareTag("Enemy"))
        //     {
        //         // other.gameObject.GetComponent<EnemyController>().DamageEnemy(10);
        //         BulletExplosion();
        //     }

            
        // }

        private void DetectByRay()
        {
            if (_rb == null) return;

            Vector2 velocity = _rb.velocity;
            if (velocity.sqrMagnitude <= Mathf.Epsilon) return;

            float distance = velocity.magnitude * Time.deltaTime;
            Vector2 origin = transform.position;
            Vector2 direction = velocity.normalized;


            RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, _layerMask);

            if(hit.collider != null)
            {
                if(hit.collider.gameObject.CompareTag("Env"))
                {
                    BulletExplosion();
                }
                else if (hit.collider.gameObject.CompareTag("Enemy"))
                {
                    BulletExplosion();
                }
            }
        }
        private void BulletExplosion()
        {
            if (_hasExploded) return;
            _hasExploded = true;

            // 停止子弹运动，并隐藏子弹模型
            _rb.velocity = Vector2.zero;
            _bulletMesh.gameObject.SetActive(false);

            // 
            Vector3 explosionRotation = transform.rotation.eulerAngles;
            explosionRotation.z += 180f;

            GameObject explosionVFX = this.GetUtility<IObjectPoolUtility>()
                        .GetObject(_pf_bulletExplosionVFX, transform.position, Quaternion.Euler(explosionRotation));

                    // 添加一次性定时器, 1秒后将子弹爆炸特效推入对象池
                    this.GetUtility<ITimerUtility>().AddOnce(
                        () => this.GetUtility<IObjectPoolUtility>().PushObject(explosionVFX),
                        1f,
                        () => this.GetUtility<IObjectPoolUtility>().PushObject(gameObject)
                    );
        }
    }
}