using System.Collections;
using QFramework;
using QFramework.Event;
using QFramework.Model;
using QFramework.System;
using QFramework.ViewController.Player;
using UnityEngine;
using UnityEngine.UI;

public class Debugers : MonoBehaviour, IController
{
    public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
    public IPlayerModel PlayerModel => this.GetModel<IPlayerModel>();
    public IEnemyInstanceSystem EnemyInstanceSystem => this.GetSystem<IEnemyInstanceSystem>();
    [Header("敌人数据")]
    [SerializeField] private int _enemyId;

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
        
        if(_enemyId != 0)
        {
            var enemyData = EnemyInstanceSystem.GetData(_enemyId);  
            info += $"Enemy Health: {enemyData.CurrentHealth.Value} / {enemyData.MaxHealth.Value}\n";
            info += $"Enemy State: {enemyData.EnemyState}\n";
            info += $"Enemy Speed: {enemyData.Speed.Value}\n";
            info += $"Enemy Size: {enemyData.enemySize.Value}\n";
            info += $"Enemy Max Health: {enemyData.MaxHealth.Value}\n";
            info += $"Enemy Current Health: {enemyData.CurrentHealth.Value}\n";
            info += $"Enemy Instance Id: {enemyData.InstanceId.Value}\n";
            info += $"Enemy Type: {enemyData.TypeEnum}\n";
        }

        _text.text = info;
    }

    private void GetEnemyId(int id)
    {
        _enemyId = id;
    }
}
