using QFramework.Command;
using QFramework.Enum;
using QFramework.Event;
using QFramework.Manager;
using QFramework.Model;
using UnityEngine;

namespace QFramework.ViewController.Mission
{
    [RequireComponent(typeof(Collider2D))]
    public class ExtractionMissionInstance : AbstractMissionInstance
    {
        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;

            var mission = MissionSystem.Missions[Index];
            if (mission.MissionState.Value != MissionState.InProgress)
                return;

            this.SendCommand(new MissionCommand.SetState(Index, MissionState.Completed));

            GameManager.Instance.SetGameResultState(GameResultState.GameFinished);
            UIGameManager.Instance.ShowPanel(UIGamePanelType.GameOverPanel);

            TypeEventSystem.Global.Send(new StatsEvent.OnMissionCompleted
            {
                Type = MissionTypeEnum.Extraction
            });
        }
    }
}
