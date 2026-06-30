using System.Collections;
using QFramework;
using QFramework.Event;
using QFramework.Manager;
using QFramework.Model;
using QFramework.System;
using QFramework.ViewController.Enemy;
using QFramework.ViewController.Player;
using UnityEngine;
using UnityEngine.UI;

public class Debugers : MonoBehaviour, IController
{
    public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
    public IPlayerModel PlayerModel => this.GetModel<IPlayerModel>();
    public IPlayerSystem PlayerSystem => this.GetSystem<IPlayerSystem>();
    public IEnemyInstanceSystem EnemyInstanceSystem => this.GetSystem<IEnemyInstanceSystem>();
    public IStatsSystem StatsSystem => this.GetSystem<IStatsSystem>();
    [Header("敌人数据")]
    [SerializeField] private int _enemyId;
    [SerializeField] private string _enemyState;
    [SerializeField] private Text _text;
    private bool _isInit = false;

    private PlayerController _playerController;

    void Start()
    {   
        StartCoroutine(Init());
    }


    void Update()
    {
        UpdateText();
    }

    IEnumerator Init()
    {
        yield return new WaitForSeconds(2f);
        _playerController = GameObject.FindAnyObjectByType<PlayerController>().GetComponent<PlayerController>();
        _isInit = true;

        TypeEventSystem.Global.Register<DebugEvent.GetEnemyId>(e => GetEnemyId(e.Id));
        TypeEventSystem.Global.Register<DebugEvent.GetEnemyState>(e => GetEnemyState(e.State));
    }


    private void UpdateText()
    {
        if(!_isInit)
        {
            return;
        }

        string info =
        $"Health: {PlayerModel.CurrentHealth.Value} / {PlayerModel.MaxHealth.Value}\n" +
        $"Fuel: {PlayerModel.CurrentFuel.Value} / {PlayerModel.MaxFuel.Value}\n" +
        $"Speed: {PlayerModel.Speed.Value}\n" +
        $"State: {_playerController.GetCurrentState()}\n";

        info += "\n";

        // 武器数据
        var pw = PlayerSystem.PlayerWeapon;
        if (pw.Left.Value != null)
        {
            info += "── Left Weapon ──\n";
            info += $"Type: {pw.Left.Value.WeaponType}\n";
            info += $"DMG: {pw.Left.Value.BulletDamage}  RPM: {pw.Left.Value.Rpm}\n";
            info += $"Mag: {pw.Left.Value.CurMagazine.Value}/{pw.Left.Value.MaxMagazine}\n";
            info += $"Ammo: {pw.Left.Value.CurMaxAmmo.Value}/{pw.Left.Value.MaxAmmo}\n";
        }
        if (pw.Right.Value != null)
        {
            info += "── Right Weapon ──\n";
            info += $"Type: {pw.Right.Value.WeaponType}\n";
            info += $"DMG: {pw.Right.Value.BulletDamage}  RPM: {pw.Right.Value.Rpm}\n";
            info += $"Mag: {pw.Right.Value.CurMagazine.Value}/{pw.Right.Value.MaxMagazine}\n";
            info += $"Ammo: {pw.Right.Value.CurMaxAmmo.Value}/{pw.Right.Value.MaxAmmo}\n";
        }

        info += "\n";

        if(_enemyId != 0)
        {
            var enemyData = EnemyInstanceSystem.GetData(_enemyId);  
            info += $"Enemy Health: {enemyData.CurrentHealth.Value} / {enemyData.MaxHealth.Value}\n";
            info += $"Enemy State: {_enemyState}\n";
            info += $"Enemy Speed: {enemyData.Speed.Value}\n";
            info += $"Enemy Size: {enemyData.enemySize.Value}\n";
            info += $"Enemy Max Health: {enemyData.MaxHealth.Value}\n";
            info += $"Enemy Current Health: {enemyData.CurrentHealth.Value}\n";
            info += $"Enemy Instance Id: {enemyData.InstanceId.Value}\n";
            info += $"Enemy Type: {enemyData.TypeEnum}\n";
            info += $"KB: {enemyData.KnockbackAccumulator.Value:F2}/{enemyData.EnemyConfig.KnockbackThreshold:F2}\n";
            info += $"Burn: {enemyData.BurnAccumulator.Value:F2}/{enemyData.EnemyConfig.BurnThreshold:F2}\n";
            info += $"Slow: {enemyData.SlowAccumulator.Value:F2}/{enemyData.EnemyConfig.SlowThreshold:F2}\n";
            var enemy = AbstractEnemy.GetById(_enemyId);
            if (enemy != null)
                info += $"KB act: {enemy.IsKnockbackActive} | Burn act: {enemy.IsBurnActive} | Slow act: {enemy.IsSlowActive}\n";
        }

        info += "\n";
        info += "── Stats ──\n";
        info += $"Kills: {StatsSystem.TotalKills}\n";
        info += $"Damage: {StatsSystem.TotalDamageDealt} dealt / {StatsSystem.TotalDamageTaken} taken\n";
        info += $"Shots: {StatsSystem.TotalShotsFired} | Acc: {StatsSystem.GetAccuracy():P0}\n";
        info += $"Time: {StatsSystem.GameTimeSeconds:F1}s\n";
        info += $"Deaths: {StatsSystem.TotalDeaths} | Missions: {StatsSystem.TotalMissionsCompleted}\n";

        _text.text = info;
    }

    private void GetEnemyId(int id)
    {
        _enemyId = id;
    }

    private void GetEnemyState(string state)
    {
        _enemyState = state;
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 200, Screen.height - 100, 180, 80));

        if(GUILayout.Button("直接通关", GUILayout.Height(40)))
        {
            DirectCompleteLevel();
        }

        GUILayout.EndArea();
    }

    private void DirectCompleteLevel()
    {
        // 强制完成信标任务
        var missionSystem = this.GetSystem<IMissionSystem>();
        if(missionSystem.Missions.Count >= 2)
        {
            var beaconMission = missionSystem.Missions[1];
            if(beaconMission.MissionState.Value != MissionState.Completed)
            {
                for(int i = 0; i < beaconMission.StepList.Count; i++)
                {
                    beaconMission.StepList[i].Value = beaconMission.MissionConfig.MissionSteps[i].Progress;
                }
                beaconMission.StepIndex.Value = beaconMission.StepList.Count;
                beaconMission.MissionState.Value = MissionState.Completed;
            }
        }

        // 强制设为完成状态 → GameManager.SetGameResultState 会自动触发存档
        GameManager.Instance.SetGameResultState(GameResultState.GameFinished);
        UIGameManager.Instance.ShowPanel(UIGamePanelType.GameOverPanel);
    }
#endif
}
