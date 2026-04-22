using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;
using QFramework.Utility;
using QFramework.ViewController.Enemy;
using Unity.VisualScripting;
using QFramework.Command;
using QFramework.Event;

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
        private int _damage;
        void FixedUpdate()
        {
            DetectByRay();
        }

        public void InitBullet(Vector3 direction, int bulletSpeed, int damage)
        {
            _rb.velocity = direction.normalized * bulletSpeed;
            _hasExploded = false;
            _bulletMesh.gameObject.SetActive(true);
            _damage = damage;
        }


        private void DetectByRay()
        {
            // 如果子弹已经爆炸，则不进行检测
            if(_hasExploded) return;

            // ----- 检测 -------------------------
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
                    int enemyId = hit.collider.TryGetComponent<EnemyController>(out var enemy) ? enemy.enemyId : -1;
                    if(enemy != null)
                    {
                        enemy.SetDeathObjectPos(transform.position);
                    }

                    this.SendCommand(EnemyCommand.Damage.Instance.Init(enemyId, _damage));
                    TypeEventSystem.Global.Send(new DebugEvent.GetEnemyId() { Id = enemyId });
                }
            }
        }
        private void BulletExplosion()
        {
            if (_hasExploded) return;
            _hasExploded = true;

            // ----- 爆炸细节 -------------------------
            _rb.velocity = Vector2.zero;
            _bulletMesh.gameObject.SetActive(false);

            var ob = this.GetUtility<IObjectPoolUtility>();
            var timer = this.GetUtility<ITimerUtility>();

            Vector3 explosionRotation = transform.rotation.eulerAngles;
            explosionRotation.z += 180f;

            GameObject explosionVFX = ob.GetObject(_pf_bulletExplosionVFX, transform.position, Quaternion.Euler(explosionRotation));

                    // 添加一次性定时器, 1秒后将子弹爆炸特效推入对象池
            timer.AddOnce(
                () => ob.PushObject(explosionVFX),
                1f,
                () => ob.PushObject(gameObject)
                );
        }
    }
}