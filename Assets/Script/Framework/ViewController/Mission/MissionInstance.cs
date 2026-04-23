using System;
using System.Collections.Generic;
using QFramework.System;
using UnityEngine;

namespace QFramework.ViewController.Mission
{
    public abstract class AbstractMissionInstance : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        public IMissionSystem MissionSystem => this.GetSystem<IMissionSystem>();
        public int Index;
        public List<Action> StepActionList;

        // TODO: 在地图生成器中，读取mission中的列表，读取物体实例化路径，实例化同时调用该初始化传入索引
        /// <summary>
        /// 初始化
        /// </summary>
        public virtual void Init(MissionDataModel mission)
        {
            Index = mission.MissionIndex;
            StepActionList = new List<Action>(mission.MissionConfig.MissionSteps.Length);      
        }

    }
}