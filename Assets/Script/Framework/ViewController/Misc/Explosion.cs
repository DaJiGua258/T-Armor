using QFramework.ViewController.Enemy;
using UnityEngine;

namespace QFramework.ViewController.Misc
{
    public class Explosion : MonoBehaviour
    {
        public float Radius;
        public int Force;
        public float Torque;
        public LayerMask LayerMask;
        public Collider2D[] Results;

        void OnEnable()
        {
            AddForce();
        }

        // public void Init(int radius, int force, int torque)
        // {
        //     Radius = radius;
        //     Force = force;
        //     Torque = torque;
        // }

        private void AddForce()
        {
            Results = Physics2D.OverlapCircleAll(transform.position, Radius, LayerMask);
            foreach(var item in Results)
            {
                if(item.CompareTag("Enemy"))
                {
                    item.TryGetComponent<EnemyController>(out var enemy);
                    enemy.ForcePush(transform.position, Force, Torque);
                }
            }
        }   
    }
}