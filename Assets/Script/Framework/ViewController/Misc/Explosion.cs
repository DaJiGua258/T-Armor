using QFramework.Event;
using QFramework.Utility;
using QFramework.ViewController.Enemy;
using UnityEngine;

namespace QFramework.ViewController.Misc
{
    public class Explosion : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        public ShakeCameraMode ShakeMode;

        public float Radius;
        public int Force;
        public float Torque;
        public LayerMask LayerMask;
        public Collider2D[] Results;

        void OnEnable()
        {
            AddForce();
        }

        private void AddForce()
        {
            Results = Physics2D.OverlapCircleAll(transform.position, Radius, LayerMask);
            foreach(var item in Results)
            {
                if(item.CompareTag("Enemy"))
                {
                    item.TryGetComponent<AbstractEnemy>(out var enemy);
                    enemy.ForcePush(transform.position, Force, Torque);
                }
            }

            TypeEventSystem.Global.Send(new ShakeCamera { strength = (int)ShakeMode });
        } 
    }

    
}