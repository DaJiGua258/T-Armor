using QFramework.Command;
using QFramework.Event;
using QFramework.Manager;
using QFramework.Model;
using QFramework.System;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class GameOverPanel : AbstractBasePanel
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _statsText;
        [SerializeField] private Button _continueBtn;
        [SerializeField] private Button _returnBtn;

        private IStatsSystem StatsSystem => this.GetSystem<IStatsSystem>();
        private IMissionSystem MissionSystemInstance => this.GetSystem<IMissionSystem>();

        public override void OnInit()
        {
            base.OnInit();

            _continueBtn.onClick.AddListener(() =>
            {
                this.SendCommand<LevelCommand.Add>(new LevelCommand.Add());
                GameManager.Instance.EnterMainScene();
            });

            _returnBtn.onClick.AddListener(() =>
            {
                GameManager.Instance.SetGameResultState(GameResultState.None);
                GameManager.Instance.EnterMainScene();
            });
        }

        public override void OnShow()
        {
            base.OnShow();

            var result = GameManager.Instance.GetGameResultState();
            _titleText.text = result == GameResultState.GameFinished ? "// FINISHED" : "// GAMEOVER";

            var missions = MissionSystemInstance.Missions;
            int completedCount = 0;
            foreach (var m in missions)
            {
                if (m.MissionState.Value == MissionState.Completed)
                    completedCount++;
            }

            float time = StatsSystem.GameTimeSeconds;
            int min = Mathf.FloorToInt(time / 60);
            int sec = Mathf.FloorToInt(time % 60);
            string timeStr = $"{min:D2}:{sec:D2}";

            string status = result == GameResultState.GameFinished ? "存活" : "死亡";

            _statsText.text =
                "> LEVEL -----------------------------------\n" +
                $"> 时间                ：{timeStr}\n" +
                $"> 完成任务           ：{completedCount}/{missions.Count}\n" +
                ">\n" +
                "> PLAYER ------------------------------\n" +
                $"> 状态                ：{status}\n" +
                $"> 开火次数           ：{StatsSystem.TotalShotsFired}\n" +
                $"> 造成伤害           ：{StatsSystem.TotalDamageDealt}\n" +
                $"> 击杀敌人           ：{StatsSystem.TotalKills}\n" +
                $"> 命中率             ：{StatsSystem.GetAccuracy():P0}\n" +
                $"> 使用支援次数     ：{StatsSystem.TotalMissionsCompleted}\n" +
                ">\n" +
                "> ----------------------------------------";
        }
    }
}
