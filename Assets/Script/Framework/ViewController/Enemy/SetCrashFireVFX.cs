using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.ViewController.Enemy
{
    public class SetCrashFireVFX : MonoBehaviour
    {
        [Header("全局启用设置")]
        [SerializeField] private float _enableChance = 1;  // 全局启用概率
        [SerializeField] private List<ParticleDuration> _particles;  // 细分特效设置
        
        void OnEnable()
        {
            float chance = UnityEngine.Random.value;
            if(chance > _enableChance)
            {
                transform.gameObject.SetActive(false);
                return;
            }
            
            // 控制细分特效时间
            StartCoroutine(PlayVFXCoroutine());
        }


        IEnumerator PlayVFXCoroutine()
        {
            foreach(var particle in _particles)
            {
                yield return new WaitForSeconds(particle.Duration);
                particle.VFX.Stop();
            }
        }
        
        [Serializable]
        public class ParticleDuration
        {
            public float Chance = 1f;  // 小于这个概率则启用
            public float Duration = 1f;
            public ParticleSystem VFX;

            public void Play()
            {
                if(UnityEngine.Random.value > Chance) return;
            }
        }
    }
}