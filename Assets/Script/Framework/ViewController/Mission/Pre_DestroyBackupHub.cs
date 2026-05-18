using QFramework.Manager;
using QFramework.System;
using QFramework.ViewController.UI;
using UnityEngine;

namespace QFramework.ViewController.Mission
{
    public class Pre_DestroyBackupHub : AbstractMissionInstance
    {
        [SerializeField] private GeneratorInteractable _generator;
        [SerializeField] private DestructibleEnv[] _cores;
        private int _phase; // 0: 侵入终端, 1: 弹出核心, 2: 摧毁核心
        private int _destroyedCoreCount;

        public override void Init(MissionDataModel mission)
        {
            base.Init(mission);
            _phase = 0;
            _destroyedCoreCount = 0;

            foreach (var core in _cores)
            {
                core.SetCanTakeDamage(false);
                core.OnDestroyed += OnCoreDestroyed;
            }

            _generator.SetCommands(
                new CommandEntry("关闭核心防护", () =>
                {
                    // 一次破解，同时推进步骤 0 和 1
                    _phase = 2;
                    AddProgress();
                    AddProgress();
                    foreach (var core in _cores)
                        core.SetCanTakeDamage(true);
                    _generator.Lock();
                }),
                new CommandEntry("", null),
                new CommandEntry("", null)
            );
        }

        private void OnCoreDestroyed()
        {
            if (_phase != 2) return;
            _destroyedCoreCount++;
            if (_destroyedCoreCount >= _cores.Length)
                AddProgress();
        }
    }
}
