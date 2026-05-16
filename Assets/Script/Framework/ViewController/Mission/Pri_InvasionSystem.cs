using QFramework.System;
using QFramework.ViewController.Player;
using UnityEngine;

namespace QFramework.ViewController.Mission
{
    public class Pri_InvasionSystem : AbstractMissionInstance
    {
        [SerializeField] private Interactable _terminal;

        public override void Init(MissionDataModel mission)
        {
            base.Init(mission);
            _terminal.OnInteracted += player => AddProgress();
        }
    }
}
