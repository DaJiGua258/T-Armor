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

        public void AddMissionProgress(MissionDataModel mission, int value);
        public void InitMission(LevelMissionTypeEnum levelMissionType);
        public MissionDataModel PrimaryMission { get; }
        public List<MissionDataModel> PrerequiredMissions { get; }
    }

    public class MissionSystem : AbstractSystem, IMissionSystem
    {
        private IMissionConfigModel _missionConfigModel => this.GetModel<IMissionConfigModel>();
        public MissionDataModel PrimaryMission { get; private set; }  // 主要任务
        public List<MissionDataModel> PrerequiredMissions { get; private set; } = new();  // 前置任务列表

        
        protected override void OnInit()
        {
            
        }

        public void InitMission(LevelMissionTypeEnum levelMissionType)
        {
            if(levelMissionType == LevelMissionTypeEnum.None)
            {
                levelMissionType = LevelMissionTypeEnum.LevelMission_1;
                UnityEngine.Debug.LogWarning("LevelMissionType is None, use LevelMission_1");
            }

            var levelConfig = _missionConfigModel.GetLevelConfig(levelMissionType);

            // ----- 初始化主要任务 -------------------------
            PrimaryMission = InitMission(levelConfig.PrimaryMissionType);

            // ----- 初始化前置任务 -------------------------
            foreach(var mission in levelConfig.PrerequiredMissionTypes)
            {
                PrerequiredMissions.Add(InitMission(mission));
            }
        }

        /// <summary>
        /// 依据类型初始化任务
        /// </summary>
        public MissionDataModel InitMission(MissionTypeEnum missionType)
        {
            // 
            var mission = new MissionDataModel(missionType);

            // 引用只读数据
            mission.MissionConfig = _missionConfigModel.GetConfig(missionType);
            
            // 初始化任务步骤
            foreach(var step in mission.MissionConfig.MissionSteps)
            {
                mission.StepList.Add(new BindableProperty<int>(0));  // 初始化任务进度
            }

            return mission;
        }

        public bool IsPreMissionFinished()
        {
            bool isAllFinished = true;
            foreach(var mission in PrerequiredMissions)
            {
                if(mission.MissionState.Value != MissionState.Completed)
                {
                    isAllFinished = false;
                    break;
                }
            }
            return isAllFinished;
        }

        public void AddMissionProgress(MissionDataModel mission, int value)
        {
            if(mission.MissionState.Value == MissionState.Completed)
            {
                UnityEngine.Debug.LogWarning("Mission is completed, cannot add progress");
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

            if(mission.StepIndex.Value > mission.MissionConfig.MissionSteps.Length - 1)
            {
                mission.MissionState.Value = MissionState.Completed;
            }
        }
    }

    /// <summary>
    /// 单个任务运行时数据
    /// </summary>
    public class MissionDataModel : InstanceType
    {
        // ----- 运行时数据 -------------------------
        private static int _missionCounter = 0;
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