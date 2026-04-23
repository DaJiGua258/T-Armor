using QFramework.Command;
using QFramework.Event;
using QFramework.Model;
using Unity.VisualScripting;
using UnityEngine;

namespace QFramework.ViewController.Mission
{
    public class PreKillEnemy : AbstractMissionInstance
    {

        void Start()
        {
            TypeEventSystem.Global.Register<MissionEvent.KillEnemyEvent>(e => AddProgress());
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if(other.CompareTag("Player"))
            {
                int missionIndex = Index;
                this.SendCommand(new MissionCommand.SetState(missionIndex, MissionState.InProgress));
                Debug.Log("当前任务状态: " + MissionSystem.Missions[missionIndex].MissionState.Value);
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if(other.CompareTag("Player"))
            {
                int missionIndex = Index;
                this.SendCommand(new MissionCommand.SetState(missionIndex, MissionState.Pause));
                Debug.Log("当前任务状态: " + MissionSystem.Missions[missionIndex].MissionState.Value);
            }
        }

        private void AddProgress()
        {
            int missionIndex = Index;
            if(MissionSystem.Missions[missionIndex].MissionState.Value != MissionState.InProgress)
            {
                return;
            }

            this.SendCommand(new MissionCommand.Add(missionIndex, 1));
        }

    }
}