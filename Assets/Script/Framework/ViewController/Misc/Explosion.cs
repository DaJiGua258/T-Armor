using QFramework.Event;
using QFramework.Utility;
using QFramework.ViewController.Enemy;
using UnityEngine;

namespace QFramework.ViewController.Misc
{
    public class Explosion : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        [Header("爆炸参数")]
        public float Radius;
        public int BaseDamage;
        public int Force;
        public float Torque;
        public LayerMask LayerMask;
        public ShakeCameraMode ShakeMode;

        [Header("衰减参数（可选）")]
        public bool UseDamageFalloff;
        public AnimationCurve DamageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        private Collider2D[] _results = new Collider2D[32];

        void OnEnable()
        {
            Explode();
        }

        private void Explode()
        {
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, Radius, _results, LayerMask);
            if (count >= _results.Length)
            {
                Debug.LogWarning($"Explosion 命中数({count})超出缓存数组({_results.Length})，部分目标未处理");
                count = _results.Length;
            }

            for (int i = 0; i < count; i++)
            {
                var col = _results[i];
                if (col == null) continue;

                // 伤害
                int finalDamage = BaseDamage;
                if (UseDamageFalloff)
                {
                    float dist = Vector2.Distance(transform.position, col.transform.position);
                    float t = Mathf.Clamp01(dist / Radius);
                    finalDamage = Mathf.RoundToInt(BaseDamage * DamageFalloff.Evaluate(t));
                }

                HitDetectionUtility.ProcessHit(col, finalDamage);

                // 物理击退（仅对 Enemy）
                if ((col.CompareTag("Enemy") || col.CompareTag("Player")) 
                    && col.TryGetComponent<AbstractEnemy>(out var enemy))
                {
                    enemy.ForcePush(transform.position, Force, Torque);
                }
            }

            // 相机震动
            TypeEventSystem.Global.Send(new ShakeCamera { strength = (int)ShakeMode });
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.15f);
            UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.forward, Radius);
        }
#endif
    }
}