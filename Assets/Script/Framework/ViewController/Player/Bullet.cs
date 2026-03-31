using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;
using QFramework.Utility;

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
            _bulletMesh.gameObject.SetActive(true);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.CompareTag("Env"))
        {
            _rb.velocity = Vector2.zero;

            _bulletMesh.gameObject.SetActive(false);

            if(_pf_bulletExplosionVFX != null)
            {
                this.GetUtility<IObjectPoolUtility>().GetObject(_pf_bulletExplosionVFX, transform.position, transform.rotation, null);
            }
        }
    }
}
}