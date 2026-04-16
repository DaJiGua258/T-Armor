using System.Collections.Generic;
using QFramework.Command;
using QFramework.Event;
using QFramework.Manager;
using QFramework.Model;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QFramework.ViewController
{
    public class Test : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                this.SendCommand(new PlayerCommand.Damage(50));
            }

            if(Input.GetKeyDown(KeyCode.B))
            {
                GameManager.Instance.SetGameResultState(GameResultState.GameFinished);
                GameManager.Instance.EnterMainScene();
            }
        }
    }
}