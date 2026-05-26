using System;
using System.Collections.Generic;
using QFramework.Enum;
using QFramework.Event;
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
        public Vector3? EntrySpawnPosition { get; set; }
    }

    public class MissionSystem : AbstractSystem, IMissionSystem
    {
        // 注释：简化后不再需要 PrimaryMissionIndex
        // private const int PrimaryMissionIndex = 1;
        private IMissionConfigModel _missionConfigModel => this.GetModel<IMissionConfigModel>();
        public List<MissionDataModel> Missions { get; private set; } = new();
        public Vector3? EntrySpawnPosition { get; set; }

        
        protected override void OnInit()
        {
            
        }

        /// <summary>
        /// 初始化关卡任务（简化：Entry → 单一玩法任务 → Extraction）
        /// </summary>
        public void InitMission(LevelMissionTypeEnum levelMissionType)
        {
            if(levelMissionType == LevelMissionTypeEnum.None)
            {
                levelMissionType = LevelMissionTypeEnum.LevMis_Beacon;
                UnityEngine.Debug.LogWarning("LevelMissionType is None, use LevMis_Beacon");
            }

            var levelConfig = _missionConfigModel.GetLevelConfig(levelMissionType);
            Missions.Clear();

            int missionIndex = 0;

            // 1) Entry 进入任务（auto-completed）
            Missions.Add(InitMission(MissionTypeEnum.Entry, MissionState.Completed, missionIndex++));

            // 2) 单一玩法任务：激活信标
            Missions.Add(InitMission(levelConfig.MissionType, MissionState.InProgress, missionIndex++));

            // 3) Extraction 撤离任务（默认未激活）
            Missions.Add(InitMission(MissionTypeEnum.Extraction, MissionState.NotStarted, missionIndex));
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

        // 注释：简化后不再需要前置任务判断
        // public bool IsPreMissionFinished() { ... }

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
                TypeEventSystem.Global.Send(new StatsEvent.OnMissionCompleted
                {
                    Type = mission.MissionType
                });
            }

            // 单一任务完成后直接尝试激活撤离
            TryActivateExtraction(mission);
        }

        private void TryActivateExtraction(MissionDataModel completedMission)
        {
            if(completedMission.MissionState.Value != MissionState.Completed)
                return;

            // 索引 1 是唯一的玩法任务
            if(completedMission.MissionIndex != 1)
                return;

            int extractionIndex = Missions.Count - 1;
            if(Missions[extractionIndex].MissionState.Value == MissionState.NotStarted)
            {
                Missions[extractionIndex].MissionState.Value = MissionState.InProgress;
            }
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