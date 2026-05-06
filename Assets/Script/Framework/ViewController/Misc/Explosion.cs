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
        public LayerMask LayerMask;
        public ShakeCameraMode ShakeMode;
        public Vector2 Offset;  // 爆炸中心偏移，默认无偏移

        [Header("衰减参数（可选）")]
        public bool UseDamageFalloff;
        public AnimationCurve DamageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        private Collider2D[] _results = new Collider2D[32];
        private Vector2 Center => (Vector2)transform.position + Offset;

        public void Init(int baseDamage, LayerMask layerMask)
        {
            BaseDamage = baseDamage;
            LayerMask = layerMask;
            Explode();
        }

        private void Start()
        {
            // 场景中直接放置时，OnEnable → Init 之前不爆炸，等手动触发
        }

        public void Explode()
        {
            int count = Physics2D.OverlapCircleNonAlloc(Center, Radius, _results, LayerMask);
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
                    float dist = Vector2.Distance(Center, col.transform.position);
                    float t = Mathf.Clamp01(dist / Radius);
                    finalDamage = Mathf.RoundToInt(BaseDamage * DamageFalloff.Evaluate(t));
                }

                HitDetectionUtility.ProcessHit(col, finalDamage);

                // 物理击退（仅对 Enemy）
                if (col.CompareTag("Enemy") || col.CompareTag("Player"))
                {
                    var enemy = col.GetComponentInParent<AbstractEnemy>();
                    if (enemy != null)
                        enemy.ForcePush(Center, (int)ShakeMode, (int)ShakeMode);
                }
            }

            // 相机震动
            TypeEventSystem.Global.Send(new ShakeCamera { strength = (int)ShakeMode });
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            // 实心填充
            UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.12f);
            UnityEditor.Handles.DrawSolidDisc(Center, Vector3.forward, Radius);

            // 边框线
            UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.6f);
            UnityEditor.Handles.DrawWireDisc(Center, Vector3.forward, Radius);

            // 偏移指示线
            if (Offset != Vector2.zero)
            {
                UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.3f);
                UnityEditor.Handles.DrawDottedLine(transform.position, Center, 2f);
            }
        }
#endif
    }
}