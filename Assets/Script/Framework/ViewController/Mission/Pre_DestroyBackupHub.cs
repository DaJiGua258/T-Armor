using QFramework.System;
using UnityEngine;

namespace QFramework.ViewController.Mission
{
    public class Pre_DestroyBackupHub : AbstractMissionInstance
    {
        [SerializeField] private GeneratorInteractable _generator;
        private int _phase; // 0: 侵入终端, 1: 弹出核心, 2: 摧毁核心

        public override void Init(MissionDataModel mission)
        {
            base.Init(mission);
            _phase = 0;

            _generator.OnInteracted += player =>
            {
                if (_phase == 0)
                {
                    // 侵入发电装置终端
                    _phase = 1;
                    AddProgress();
                }
                else if (_phase == 1)
                {
                    // 弹出散热核心
                    _phase = 2;
                    AddProgress();
                }
            };

            // Step 3: 摧毁散热核心 — 需要玩家射击摧毁，待实现
        }
    }
}
