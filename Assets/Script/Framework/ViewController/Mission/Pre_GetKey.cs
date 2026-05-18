using QFramework.Manager;
using QFramework.System;
using QFramework.ViewController.Player;
using QFramework.ViewController.UI;
using UnityEngine;

namespace QFramework.ViewController.Mission
{
    public class Pre_GetKey : AbstractMissionInstance
    {
        [SerializeField] private Interactable _keySocket;
        [SerializeField] private Interactable _terminal;
        [SerializeField] private Grabbable _keyGrabbable;

        public override void Init(MissionDataModel mission)
        {
            base.Init(mission);

            // 必须先插入密钥，才能使用终端
            _terminal.Lock();

            _keySocket.OnObjectPlaced += () =>
            {
                AddProgress();
                _keyGrabbable.Lock();
                _keySocket.Lock();
                _terminal.Unlock();
            };

            _terminal.OnInteracted += player =>
            {
                var panel = UIGameManager.Instance.GetComponentInChildren<TerminalPanel>(true);
                panel.ShowWithCommands(
                    new CommandEntry("入侵节点电脑", () =>
                    {
                        AddProgress();
                        _terminal.Lock();
                    }),
                    new CommandEntry("", null),
                    new CommandEntry("", null)
                );
            };
        }
    }
}
