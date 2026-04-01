using QFramework.Command;
using QFramework.Model;
using UnityEngine;

namespace QFramework.ViewController
{
    public class Test : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        public int enemyId;
        public IEnemeyDataModel enemeyDataModel => this.GetModel<IEnemeyDataModel>();

        public void Update()   
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                enemyId = this.SendCommand(new EnemyCommand.Add());
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                var enemy = this.enemeyDataModel.GetEnemyFromCache(enemyId);
                this.SendCommand(new EnemyCommand.Damage(enemyId, 15));
                Debug.Log("敌人当前生命值：" + enemy.CurrentHealth.Value);
            }
        }

        public void Damage()
        {

        }
    }
}