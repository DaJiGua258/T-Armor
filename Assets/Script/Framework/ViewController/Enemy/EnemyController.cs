using QFramework;
using QFramework.Command;
using QFramework.Model;
using QFramework.Enum;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public class EnemyController : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        public int enemyId;
        public EnemyTypeEnum enemyEnum;

        void Start()
        {
            enemyId = this.SendCommand(new EnemyCommand.Add(enemyEnum, enemyId));
        }


        public void Update()   
        {
            
        }

        public void DamageEnemy(int damage)
        {
            this.SendCommand(new EnemyCommand.Damage(enemyId, damage));
        }
        
    }
}