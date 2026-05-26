// 注释：简化后不再使用，保留代码以便后续复用
// using QFramework.Manager;
// using QFramework.System;
// using QFramework.ViewController.Player;
// using QFramework.ViewController.UI;
// using UnityEngine;

// namespace QFramework.ViewController.Mission
// {
//     public class Pre_GetKey : AbstractMissionInstance
//     {
//         [SerializeField] private Interactable _keySocket;
//         [SerializeField] private Interactable _terminal;
//         [SerializeField] private Grabbable _keyGrabbable;

//         public override void Init(MissionDataModel mission)
//         {
//             base.Init(mission);

//             _terminal.Lock();

//             _keySocket.OnObjectPlaced += () =>
//             {
//                 AddProgress();
//                 _keyGrabbable.Lock();
//                 _keySocket.Lock();
//                 _terminal.Unlock();
//             };

//             _terminal.OnInteracted += player =>
//             {
//                 var panel = UIGameManager.Instance.GetComponentInChildren<TerminalPanel>(true);
//                 panel.ShowWithCommands(
//                     new CommandEntry("入侵节点电脑", () =>
//                     {
//                         AddProgress();
//                         _terminal.Lock();
//                     }),
//                     new CommandEntry("", null),
//                     new CommandEntry("", null)
//                 );
//             };
//         }
//     }
// }
