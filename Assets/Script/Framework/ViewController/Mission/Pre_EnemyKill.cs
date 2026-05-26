// 注释：简化后不再使用，保留代码以便后续复用
// using QFramework.Command;
// using QFramework.Event;
// using QFramework.Model;
// using QFramework.ViewController.Enemy;
// using System.Collections.Generic;
// using Unity.VisualScripting;
// using UnityEngine;

// namespace QFramework.ViewController.Mission
// {
//     public class Pre_EnemyKill : AbstractMissionInstance
//     {
//         private HashSet<int> _enemyIdsInRange = new HashSet<int>();

//         void Start()
//         {
//             TypeEventSystem.Global.Register<StatsEvent.OnEnemyKilled>(OnEnemyKilled)
//                 .UnRegisterWhenGameObjectDestroyed(gameObject);

//             ScanEnemiesInRange();
//         }

//         private void OnEnemyKilled(StatsEvent.OnEnemyKilled e)
//         {
//             if (!_enemyIdsInRange.Contains(e.EnemyId))
//                 return;

//             int missionIndex = Index;
//             if (MissionSystem.Missions[missionIndex].MissionState.Value != MissionState.InProgress)
//                 return;

//             this.SendCommand(new MissionCommand.Add(missionIndex, 1));
//         }

//         void OnTriggerEnter2D(Collider2D other)
//         {
//             if (other.CompareTag("Player"))
//             {
//                 int missionIndex = Index;
//                 this.SendCommand(new MissionCommand.SetState(missionIndex, MissionState.InProgress));
//                 return;
//             }

//             var enemy = other.GetComponentInParent<AbstractEnemy>();
//             if (enemy != null)
//                 _enemyIdsInRange.Add(enemy.enemyId);
//         }

//         void OnTriggerExit2D(Collider2D other)
//         {
//             if (other.CompareTag("Player"))
//             {
//                 int missionIndex = Index;
//                 this.SendCommand(new MissionCommand.SetState(missionIndex, MissionState.Pause));
//                 return;
//             }

//             var enemy = other.GetComponentInParent<AbstractEnemy>();
//             if (enemy != null)
//                 _enemyIdsInRange.Remove(enemy.enemyId);
//         }

//         private void ScanEnemiesInRange()
//         {
//             var triggerCollider = GetComponent<Collider2D>();
//             if (triggerCollider == null) return;

//             var results = new Collider2D[20];
//             int count = Physics2D.OverlapCollider(triggerCollider, new ContactFilter2D().NoFilter(), results);
//             for (int i = 0; i < count; i++)
//             {
//                 var enemy = results[i].GetComponentInParent<AbstractEnemy>();
//                 if (enemy != null)
//                     _enemyIdsInRange.Add(enemy.enemyId);
//             }
//         }

//     }
// }
