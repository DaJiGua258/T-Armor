using System.Collections.Generic;
using QFramework.Model;
using QFramework.Event;
using QFramework.Utility;
using System;
using UnityEngine;
using QFramework.Enum;
using QFramework;
using QFramework.UtilityKit;

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
        private IMissionSystem _missionSystem => this.GetSystem<IMissionSystem>();
        private IMissionConfigModel _missionConfigModel => this.GetModel<IMissionConfigModel>();
        // ----- 已经通过的关卡缓存 -------------------------
        public List<LevelDataModel> LevelDataCache { get; private set; } = new List<LevelDataModel>();

        // ----- 当前选中的关卡数据 -------------------------
        public LevelDataModel LoadedLevelData { get; private set; }

        


        protected override void OnInit()
        {
            LoadedLevelData = new LevelDataModel();
            _missionSystem.InitMission(LoadedLevelData.LevelMissionConfig.LevelMissionType);
        }

        public void AddLoadLevel()
        {
            LevelDataCache.Add(LoadedLevelData);

            // 加入关卡后，才进行真正的数据加载
            _missionSystem.InitMission(LoadedLevelData.LevelMissionConfig.LevelMissionType);
        }

    
        public void InitLevelEnv(PlanetNodeMapData nodeData)
        {
            LoadedLevelData = new LevelDataModel();
            LoadedLevelData.seed.Value = nodeData.Seed;
            LoadedLevelData.EnvironmentData.terrainType = nodeData.environmentData.terrainType;
            LoadedLevelData.EnvironmentData.moistureType = nodeData.environmentData.moistureType;
            LoadedLevelData.EnvironmentData.plantLevelType = nodeData.environmentData.plantLevelType;
            LoadedLevelData.EnvironmentData.SurfaceNormal = nodeData.environmentData.SurfaceNormal;

            // 从已配置的关卡任务类型中随机选取，并填充关卡名称/描述
            var availableTypes = _missionConfigModel.GetAvailableLevelMissionTypes();
            LevelMissionTypeEnum levelMissionType = availableTypes[SeedRandom.Range(0, availableTypes.Count)];
            var levelConfig = _missionConfigModel.GetLevelConfig(levelMissionType);
            LoadedLevelData.LevelMissionConfig.LevelName = levelConfig.LevelName;
            LoadedLevelData.LevelMissionConfig.LevelDescription = levelConfig.LevelDescription;
            LoadedLevelData.LevelMissionConfig.LevelMissionType = levelConfig.LevelMissionType;

            this.SendEvent<UpdateMapInfo>(new UpdateMapInfo());
        }
    }   

    public class LevelDataModel
    {
        // ----- 关卡信息 -------------------------
        public LevelMissionConfig LevelMissionConfig = new LevelMissionConfig();

        // ----- 地图信息 -------------------------
        public BindableProperty<int> seed = new BindableProperty<int>();  // 地图种子
        public EnvironmentData EnvironmentData = new EnvironmentData();
    }

    

    [Serializable]
    public class EnvironmentData
    {
        public Vector3 SurfaceNormal;
        public TerrainType terrainType;
        public MoistureType moistureType;
        public PlantLevelType plantLevelType;
    }

    
        

}