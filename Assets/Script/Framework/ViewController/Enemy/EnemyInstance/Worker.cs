using QFramework.Command;
using QFramework.System;
using QFramework.ViewController.Player;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public class Worker : AbstractEnemy
    {
        [Header("特殊引用")]
        [SerializeField] private ElecShock _light;

        public override void Attack()
        {
            Shoot();
        }

        public override void Shoot()
        {
            _light.Draw(Target);
            if(Target.TryGetComponent<PlayerController>(out PlayerController c))
            {
                this.SendCommand(PlayerCommand.Damage.Instance.Init(EnemyInstanceSystem.GetData(enemyId).Damage));
            }
            // var obj = Instantiate(_pf_bullet, Muzzle.position, Muzzle.rotation);
        }
    }
}
