using QFramework.Command;
using QFramework.Model;
using UnityEngine;

namespace QFramework.ViewController
{
    public class Test : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        public int enemyId;
        public IEnemeyConfigModel enemeyDataModel => this.GetModel<IEnemeyConfigModel>();

        public void Update()   
        {

        }

        public void Damage()
        {
   
        }
    }
}