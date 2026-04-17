using System.Collections.Generic;
using QFramework.Model;
using QFramework.Event;
using QFramework.Utility;
using System;
using UnityEngine;
using QFramework.Enum;
using QFramework;

namespace QFramework.System
{
    public interface ILevelSystem : ISystem
    {
        public LevelDataModel LoadedLevelData { get; }  // 当前选中的关卡数据，用于后续加载读取（选关界面的数据）
        public List<LevelDataModel> LevelDataCache { get; }  // 当前已加载的关卡数据
        
        public void AddLoadLevel();
        public void InitLevelEnv(PlanetNodeMapData nodeData);
    }

    public class LevelSystem : AbstractSystem, ILevelSystem
    {
        private IMissionConfigModel _missionConfigModel => this.GetModel<IMissionConfigModel>();
        // 已经通过的关卡缓存
        public List<LevelDataModel> LevelDataCache { get; private set; } = new List<LevelDataModel>();
        public LevelDataModel LoadedLevelData { get; private set; }  // 当前已加载的【关卡数据】

        public List<LevelMissionDataModel> MissionDataCache { get; private set; } = new List<LevelMissionDataModel>()
        {
            new LevelMissionDataModel(
                "Level_1",  // 关卡名称
                "Level_1 Description",  // 关卡描述
                MissionTypeEnum.Mission_1,  // 主要任务
                new List<MissionTypeEnum>() { MissionTypeEnum.Mission_2, MissionTypeEnum.Mission_3 }  // 前置任务
            ),
        };


        protected override void OnInit()
        {
            
        }

        public void AddLoadLevel()
        {
            LevelDataCache.Add(LoadedLevelData);
        }

    
        public void InitLevelEnv(PlanetNodeMapData nodeData)
        {
            LoadedLevelData = new LevelDataModel();
            LoadedLevelData.seed.Value = nodeData.Seed;
            LoadedLevelData.EnvironmentData.terrainType = nodeData.environmentData.terrainType;
            LoadedLevelData.EnvironmentData.moistureType = nodeData.environmentData.moistureType;
            LoadedLevelData.EnvironmentData.plantLevelType = nodeData.environmentData.plantLevelType;
            LoadedLevelData.EnvironmentData.SurfaceNormal = nodeData.environmentData.SurfaceNormal;

            this.SendEvent<UpdateMapInfo>(new UpdateMapInfo());
        }

        public void InitLevelMission()
        {
            var levelMission = LoadedLevelData.LevelMissionData;
            
            levelMission.PrimaryMission = 
                new MissionDataModel(_missionConfigModel.GetConfig(levelMission.PrimaryMissionType));


            foreach(var missionType in levelMission.PrerequiredMissionTypes)
            {
                levelMission.PrerequiredMissions.Add(
                    new MissionDataModel(_missionConfigModel.GetConfig(missionType))
                );
            }

        }

    }   

    public class LevelDataModel
    {
        public BindableProperty<int> seed = new BindableProperty<int>();  // 地图种子
        public EnvironmentData EnvironmentData = new EnvironmentData();
        public LevelMissionDataModel LevelMissionData;  // 关卡任务数据

        /// <summary>
        /// 前置任务是否全部完成
        /// </summary>
        public bool IsPreMissionFinished()
        {
            bool isAllFinished = true;
            foreach(var mission in LevelMissionData.PrerequiredMissions)
            {
                if(mission.MissionState != MissionState.Completed)
                {
                    isAllFinished = false;
                    break;
                }
            }
            return isAllFinished;
        }

        
    }

    public class LevelMissionDataModel
    {
        public string LevelName;  // 关卡名称
        public string LevelDescription;  // 关卡描述

        public MissionTypeEnum PrimaryMissionType;
        public MissionDataModel PrimaryMission;  // 主要任务
        
        public List<MissionTypeEnum> PrerequiredMissionTypes = new();
        public List<MissionDataModel> PrerequiredMissions = new();  // 前置任务列表

        public LevelMissionDataModel(
            string levelName,
            string levelDescription,
            MissionTypeEnum primaryMissionType,
            List<MissionTypeEnum> prerequiredMissionTypes
        )
        {
            LevelName = levelName;
            LevelDescription = levelDescription;
            PrimaryMissionType = primaryMissionType;
            PrerequiredMissionTypes = prerequiredMissionTypes;
        }
    }

    [Serializable]
    public class EnvironmentData
    {
        public Vector3 SurfaceNormal;
        public TerrainType terrainType;
        public MoistureType moistureType;
        public PlantLevelType plantLevelType;
    }

    /// <summary>
    /// 单个任务运行时数据
    /// </summary>
    public class MissionDataModel : InstanceType
    {
        // ----- 运行时数据 -------------------------
        private static int _missionCounter = 0;
        public MissionTypeEnum MissionType;
        public MissionState MissionState;  // 当前任务状态


        // ----- 只读的任务信息 -------------------------
        public Sprite MissionIcon;
        public string MissionName;  // 任务名称
        public string[] TipText;  // 任务提示文本
        public string MissionDescription;  // 任务描述

        // 任务实例对象（生成玩家可交互的游戏物体，如建筑等）
        public GameObject MissionInstanceObject;


        public MissionDataModel(MissionConfig missionConfig)
        {
            this.InstanceId.Value = GetInstanceId((int)MissionType, _missionCounter);
            _missionCounter++;

            MissionType = missionConfig.MissionType;
            MissionIcon = missionConfig.MissionIcon;
            MissionName = missionConfig.MissionName;
            TipText = missionConfig.TipText;
            MissionDescription = missionConfig.MissionDescription;
            MissionInstanceObject = missionConfig.MissionInstanceObject;
            MissionState = MissionState.NotStarted;
        }
    }
        

}