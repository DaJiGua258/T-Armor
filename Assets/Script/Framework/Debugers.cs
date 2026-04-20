using System.Collections;
using QFramework;
using QFramework.Model;
using QFramework.System;
using QFramework.ViewController.Player;
using UnityEngine;
using UnityEngine.UI;

public class Debugers : MonoBehaviour, IController
{
    public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
    public IPlayerModel PlayerModel => this.GetModel<IPlayerModel>();

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

        _text.text = info;
    }
}
