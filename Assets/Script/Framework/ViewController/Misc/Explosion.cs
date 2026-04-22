using QFramework.ViewController.Enemy;
using UnityEngine;

namespace QFramework.ViewController.Misc
{
    public class Explosion : MonoBehaviour
    {
        public int Radius;
        public int Force;
        public LayerMask LayerMask;
        public Collider2D[] Results;

        void OnEnable()
        {
            AddForce();
        }

        public void Init(int radius, int force)
        {
            Radius = radius;
            Force = force;
        }

        private void AddForce()
        {
            Results = Physics2D.OverlapCircleAll(transform.position, Radius, LayerMask);
            foreach(var item in Results)
            {
                if(item.CompareTag("Enemy"))
                {
                    item.TryGetComponent<EnemyController>(out var enemy);
                    enemy.ForcePush(transform.position, Force);
                }
            }
        }   
    }
}