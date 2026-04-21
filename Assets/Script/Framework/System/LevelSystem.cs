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
        // ----- 已经通过的关卡缓存 -------------------------
        public List<LevelDataModel> LevelDataCache { get; private set; } = new List<LevelDataModel>();

        // ----- 当前选中的关卡数据 -------------------------
        public LevelDataModel LoadedLevelData { get; private set; }

        


        protected override void OnInit()
        {
            LoadedLevelData = new LevelDataModel();
            _missionSystem.InitMission(LoadedLevelData.LevelMissionType);
        }

        public void AddLoadLevel()
        {
            LevelDataCache.Add(LoadedLevelData);

            // 加入关卡后，才进行真正的数据加载
            _missionSystem.InitMission(LoadedLevelData.LevelMissionType);
        }

    
        public void InitLevelEnv(PlanetNodeMapData nodeData)
        {
            LoadedLevelData = new LevelDataModel();
            LoadedLevelData.seed.Value = nodeData.Seed;
            LoadedLevelData.EnvironmentData.terrainType = nodeData.environmentData.terrainType;
            LoadedLevelData.EnvironmentData.moistureType = nodeData.environmentData.moistureType;
            LoadedLevelData.EnvironmentData.plantLevelType = nodeData.environmentData.plantLevelType;
            LoadedLevelData.EnvironmentData.SurfaceNormal = nodeData.environmentData.SurfaceNormal;

            // 通过种子获取关卡任务类型
            LevelMissionTypeEnum levelMissionType = (LevelMissionTypeEnum)SeedRandom
                .Range(1, global::System.Enum.GetNames(typeof(LevelMissionTypeEnum)).Length);

            this.SendEvent<UpdateMapInfo>(new UpdateMapInfo());
        }
    }   

    public class LevelDataModel
    {
        // ----- 关卡信息 -------------------------
        public string LevelName;  // 关卡名称
        public string LevelDescription;  // 关卡描述
        public LevelMissionTypeEnum LevelMissionType = LevelMissionTypeEnum.None;  // 关卡任务类型

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