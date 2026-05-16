using QFramework.Command;
using QFramework.System;
using QFramework.Model;
using UnityEngine;

namespace QFramework.ViewController.Mission
{
    [RequireComponent(typeof(Collider2D))]
    public abstract class AbstractMissionInstance : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        public int Index { get; private set; }
        protected IMissionSystem MissionSystem => this.GetSystem<IMissionSystem>();
        private Collider2D _collider;

        /// <summary>
        /// 初始化
        /// </summary>
        public virtual void Init(MissionDataModel mission)
        {
            _collider = GetComponent<Collider2D>();
            Index = mission.MissionIndex;
        }

        protected void AddProgress(int amount = 1)
        {
            this.SendCommand(new MissionCommand.Add(Index, amount));
        }

        protected void SetState(MissionState state)
        {
            this.SendCommand(new MissionCommand.SetState(Index, state));
        }
    }
}