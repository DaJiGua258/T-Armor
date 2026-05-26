// 注释：简化后不再使用，保留代码以便后续复用
// using System.Collections;
// using QFramework.Event;
// using QFramework.Manager;
// using QFramework.System;
// using QFramework.ViewController.Player;
// using QFramework.ViewController.UI;
// using UnityEngine;

// namespace QFramework.ViewController.Mission
// {
//     public class Pri_InvasionSystem : AbstractMissionInstance
//     {
//         [SerializeField] private Interactable _terminal;
//         [SerializeField] private Transform _invasionDropPoint;
//         [SerializeField] private float _hackDuration = 60f;

//         private enum Phase { Idle, Cracking, FinalHack, Done }
//         private Phase _phase;
//         private Coroutine _crackTimer;

//         public override void Init(MissionDataModel mission)
//         {
//             base.Init(mission);
//             _phase = Phase.Idle;
//             _terminal.OnInteracted += OnTerminalInteracted;
//         }

//         private void OnTerminalInteracted(GameObject player)
//         {
//             switch (_phase)
//             {
//                 case Phase.Idle:
//                     ShowFirstHackTerminal();
//                     break;
//                 case Phase.FinalHack:
//                     ShowSecondHackTerminal();
//                     break;
//             }
//         }

//         private void ShowFirstHackTerminal()
//         {
//             var panel = UIGameManager.Instance.GetComponentInChildren<TerminalPanel>(true);
//             panel.ShowWithCommands(
//                 new CommandEntry("破解防御网络", OnFirstHackComplete),
//                 new CommandEntry("", null),
//                 new CommandEntry("", null)
//             );
//         }

//         private void OnFirstHackComplete()
//         {
//             _phase = Phase.Cracking;
//             AddProgress();
//             _terminal.Lock();

//             EnemySpawnerManager.Instance.StartTimedWaves(_hackDuration, _invasionDropPoint);

//             TypeEventSystem.Global.Send(new InvasionTimerEvent
//             {
//                 Active = true,
//                 Duration = _hackDuration,
//                 TerminalWorldPos = _terminal.transform.position
//             });

//             _crackTimer = StartCoroutine(CrackTimerRoutine());
//         }

//         private IEnumerator CrackTimerRoutine()
//         {
//             yield return new WaitForSeconds(_hackDuration);
//             OnCrackTimeUp();
//         }

//         private void OnCrackTimeUp()
//         {
//             if (_phase != Phase.Cracking) return;

//             EnemySpawnerManager.Instance.StopTimedWaves();
//             TypeEventSystem.Global.Send(new InvasionTimerEvent { Active = false });

//             _phase = Phase.FinalHack;
//             AddProgress();
//             _terminal.Unlock();
//         }

//         private void ShowSecondHackTerminal()
//         {
//             var panel = UIGameManager.Instance.GetComponentInChildren<TerminalPanel>(true);
//             panel.ShowWithCommands(
//                 new CommandEntry("注入干扰程序", OnSecondHackComplete),
//                 new CommandEntry("", null),
//                 new CommandEntry("", null)
//             );
//         }

//         private void OnSecondHackComplete()
//         {
//             _phase = Phase.Done;
//             AddProgress();
//             _terminal.Lock();
//         }
//     }
// }
