using System;
using System.Collections.Generic;
using QFramework.Enum;
using QFramework.Model;
using QFramework.UtilityKit;
using UnityEngine;

namespace QFramework.System
{
    public interface IMissionSystem : ISystem
    {
        public void AddProgress(MissionDataModel mission, int value);
        public void InitMission(LevelMissionTypeEnum levelMissionType);
        public List<MissionDataModel> Missions { get; }
    }

    public class MissionSystem : AbstractSystem, IMissionSystem
    {
        private const int PrimaryMissionIndex = 0;
        private IMissionConfigModel _missionConfigModel => this.GetModel<IMissionConfigModel>();
        public List<MissionDataModel> Missions { get; private set; } = new();

        
        protected override void OnInit()
        {
            
        }

        /// <summary>
        /// 初始化关卡任务
        /// </summary>
        public void InitMission(LevelMissionTypeEnum levelMissionType)
        {
            if(levelMissionType == LevelMissionTypeEnum.None)
            {
                levelMissionType = LevelMissionTypeEnum.LevMis_CleaArea;
                UnityEngine.Debug.LogWarning("LevelMissionType is None, use LevelMission_1");
            }

            var levelConfig = _missionConfigModel.GetLevelConfig(levelMissionType);
            Missions.Clear();

            for(int missionIndex = 0; missionIndex < levelConfig.MissionTypes.Count; missionIndex++)
            {
                var missionType = levelConfig.MissionTypes[missionIndex];
                var initState = MissionState.NotStarted;
                Missions.Add(InitMission(missionType, initState, missionIndex));
            }
        }

        /// <summary>
        /// 依据类型初始化任务
        /// </summary>
        public MissionDataModel InitMission(MissionTypeEnum missionType, MissionState state, int missionIndex)
        {
            // 
            var mission = new MissionDataModel(missionType);
            mission.MissionIndex = missionIndex;

            // 引用只读数据
            mission.MissionConfig = _missionConfigModel.GetConfig(missionType);
            
            // 初始化任务步骤
            foreach(var step in mission.MissionConfig.MissionSteps)
            {
                mission.StepList.Add(new BindableProperty<int>(0));  // 初始化任务进度
            }

            mission.MissionState.Value = state;

            return mission;
        }

        /// <summary>
        /// 判断前置任务是否全部完成
        /// </summary>
        public bool IsPreMissionFinished()
        {
            for(int missionIndex = 1; missionIndex < Missions.Count; missionIndex++)
            {
                var mission = Missions[missionIndex];
                if(mission.MissionState.Value != MissionState.Completed)
                {
                    return false;
                }
            }

            if(Missions.Count <= PrimaryMissionIndex)
            {
                return false;
            }

            var priMission = Missions[PrimaryMissionIndex];
            if(priMission.MissionType != MissionTypeEnum.None &&
               priMission.MissionState.Value == MissionState.NotStarted)
            {
                priMission.MissionState.Value = MissionState.InProgress;
            }

            return true;
        }

        public void AddProgress(MissionDataModel mission, int value)
        {
            if(mission.MissionState.Value == MissionState.Completed || mission.MissionState.Value == MissionState.Pause)
            {
                UnityEngine.Debug.LogWarning("任务状态异常（已完成或暂停），无法添加进度");
                return;
            }

            // 当前任务阶段
            int step = mission.StepIndex.Value;

            // 当前任务阶段进度
            mission.StepList[step].Value += value;

            // 如果当前任务阶段进度大于等于配置中的目标
            if(mission.StepList[step].Value >= mission.MissionConfig.MissionSteps[step].Progress)
            {
                mission.StepIndex.Value++;
            }

            // 如果当前任务阶段索引大于等于配置中的总阶段数，则任务完成
            if(mission.StepIndex.Value > mission.MissionConfig.MissionSteps.Length - 1)
            {
                mission.MissionState.Value = MissionState.Completed;
            }

            // 判断前置任务是否全部完成
            IsPreMissionFinished();
        }

    }

    /// <summary>
    /// 单个任务运行时数据
    /// </summary>
    public class MissionDataModel
    {
        // ----- 运行时数据 -------------------------
        public int MissionIndex = -1;
        public MissionTypeEnum MissionType;
        public BindableProperty<MissionState> MissionState;  // 当前任务状态
        public List<BindableProperty<int>> StepList = new();  // 任务阶段进度，依据索引访问当前任务进度
        public BindableProperty<int> StepIndex;  // 任务阶段索引，标记当前任务进行到哪一步


        // ----- 只读的任务信息 -------------------------
        public MissionConfig MissionConfig;
        // public Sprite MissionIcon;
        // public string MissionName;  // 任务名称
        // public string[] TipText;  // 任务提示文本
        // public string MissionDescription;  // 任务描述

        // // 任务实例对象（生成玩家可交互的游戏物体，如建筑等）
        // public GameObject MissionInstanceObject;

        public MissionDataModel(MissionTypeEnum missionType)
        {
            MissionType = missionType;
            MissionState = new BindableProperty<MissionState>();
            StepIndex = new BindableProperty<int>(0);
        }

        // public MissionDataModel(MissionConfig missionConfig)
        // {
        //     this.InstanceId.Value = GetInstanceId((int)MissionType, _missionCounter);
        //     _missionCounter++;

        //     MissionType = missionConfig.MissionType;
        //     MissionIcon = missionConfig.MissionIcon;
        //     MissionName = missionConfig.MissionName;
        //     TipText = missionConfig.TipText;
        //     MissionDescription = missionConfig.MissionDescription;
        //     MissionInstanceObject = missionConfig.MissionInstanceObject;
        //     MissionState = MissionState.NotStarted;
        // }
    }
}