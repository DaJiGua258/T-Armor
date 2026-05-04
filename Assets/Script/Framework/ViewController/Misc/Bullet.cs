using UnityEngine;
using QFramework.Event;
using QFramework.ViewController.Misc;

namespace QFramework.ViewController.Player
{
    public class Bullet : AbstractBullet
    {
        void Start()
        {
            TypeEventSystem.Global.Register<WeaponEvent.UpdateBulletLayerMask>(OnUpdateBulletLayerMask);
        }

        private void OnUpdateBulletLayerMask(WeaponEvent.UpdateBulletLayerMask e)
        {
            _layerMask = e.LayerMask;
        }

        protected override void Detect()
        {
            Vector2 velocity = _rb.velocity;
            if (velocity.sqrMagnitude <= Mathf.Epsilon) return;

            float distance = velocity.magnitude * Time.deltaTime + 0.5f;
            Vector2 origin = transform.position;
            Vector2 direction = velocity.normalized;

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, _layerMask);
            Debug.DrawRay(origin, direction * distance, Color.red);

            if (hit.collider != null && TagMatches(hit.collider.tag))
            {
                Explode(hit.point);
                HitDetectionUtility.ProcessHit(hit.collider, _damage);
            }
        }
    }
}
