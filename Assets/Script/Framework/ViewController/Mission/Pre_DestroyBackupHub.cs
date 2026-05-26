// 注释：简化后不再使用，保留代码以便后续复用
// using QFramework.Manager;
// using QFramework.System;
// using QFramework.ViewController.UI;
// using UnityEngine;

// namespace QFramework.ViewController.Mission
// {
//     public class Pre_DestroyBackupHub : AbstractMissionInstance
//     {
//         [SerializeField] private GeneratorInteractable _generator;
//         [SerializeField] private DestructibleEnv[] _cores;
//         private int _phase;
//         private int _destroyedCoreCount;

//         public override void Init(MissionDataModel mission)
//         {
//             base.Init(mission);
//             _phase = 0;
//             _destroyedCoreCount = 0;

//             foreach (var core in _cores)
//             {
//                 core.SetCanTakeDamage(false);
//                 core.OnDestroyed += OnCoreDestroyed;
//             }

//             _generator.SetCommands(
//                 new CommandEntry("关闭核心防护", () =>
//                 {
//                     _phase = 2;
//                     AddProgress();
//                     AddProgress();
//                     foreach (var core in _cores)
//                         core.SetCanTakeDamage(true);
//                     _generator.Lock();
//                 }),
//                 new CommandEntry("", null),
//                 new CommandEntry("", null)
//             );
//         }

//         private void OnCoreDestroyed()
//         {
//             if (_phase != 2) return;
//             _destroyedCoreCount++;
//             if (_destroyedCoreCount >= _cores.Length)
//                 AddProgress();
//         }
//     }
// }
