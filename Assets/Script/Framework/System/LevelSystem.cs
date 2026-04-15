using System.Collections.Generic;
using QFramework.Model;
using QFramework.Event;
using QFramework.Utility;
using System;
using UnityEngine;

namespace QFramework.System
{
    public interface ILevelSystem : ISystem
    {
        public LevelDataModel CurrentSelectLevelData { get; }  // 当前选中的关卡数据（选关界面的数据）
        public LevelDataModel LoadedLevelData { get;}  // 当前读取的关卡数据（实际加载关卡后的数据）


        public void AddLevel(LevelDataModel levelData);
        public void SelectedLevel(PlanetNodeMapData nodeData);
        public void CommitSelectedLevelAsLoaded();

        
        public MapGeneratorParametersSO GeMapConfigFromLevelData();
    }

    public class LevelSystem : AbstractSystem, ILevelSystem
    {
        public LevelDataModel CurrentSelectLevelData { get; private set; }  // 当前已加载的关卡数据
        public LevelDataModel LoadedLevelData { get; private set; }  // 当前读取的关卡数据
        private List<LevelDataModel> _levelDataCache = new List<LevelDataModel>();

        

        protected override void OnInit()
        {
            CurrentSelectLevelData = new LevelDataModel();
            LoadedLevelData = new LevelDataModel();
        }

        public void LoadLevel(int index)
        {
            
        }

        public void AddLevel(LevelDataModel levelData)
        {
            _levelDataCache.Add(levelData);
        }

    
        public void SelectedLevel(PlanetNodeMapData nodeData)
        {
            CurrentSelectLevelData.seed.Value = nodeData.Seed;
            CurrentSelectLevelData.environmentData.terrainType = nodeData.environmentData.terrainType;
            CurrentSelectLevelData.environmentData.moistureType = nodeData.environmentData.moistureType;
            CurrentSelectLevelData.environmentData.plantLevelType = nodeData.environmentData.plantLevelType;

            this.SendEvent<UpdateMapInfo>(new UpdateMapInfo());
        }

        public void CommitSelectedLevelAsLoaded()
        {
            if (CurrentSelectLevelData == null)
            {
                Debug.LogWarning("LevelSystem: CurrentSelectLevelData is null, cannot commit selected level.");
                return;
            }

            LoadedLevelData.CopyDataFrom(CurrentSelectLevelData);
        }

        /// <summary>
        /// 从当前加载的关卡数据加载地图信息
        /// </summary>
        public MapGeneratorParametersSO GeMapConfigFromLevelData()
        {
            string path = "SOData/MapConfig/";
            path += LoadedLevelData.environmentData.terrainType.ToString();

            var mapConfig = this.GetUtility<IResourceLoad>().Load<MapGeneratorParametersSO>(path);
            if (mapConfig == null)
            {
                Debug.LogWarning($"LevelSystem: Map config not found at path '{path}'.");
            }

            return mapConfig;
        }
    }   

    public class LevelDataModel
    {
        public BindableProperty<int> seed = new BindableProperty<int>();  // 地图种子
        public EnvironmentData environmentData = new EnvironmentData();
        public int LevelIndex = -1;  // 关卡索引

        public LevelDataModel() { }

        public LevelDataModel(int seed, TerrainType terrainTierType, MoistureType moistureBandType, PlantLevelType plantLevelType)
        {
            this.seed.Value = seed;
            environmentData.terrainType = terrainTierType;
            environmentData.moistureType = moistureBandType;
            environmentData.plantLevelType = plantLevelType;
        }

        public void CopyDataFrom(LevelDataModel levelData)
        {
            seed.Value = levelData.seed.Value;
            environmentData.terrainType = levelData.environmentData.terrainType;
            environmentData.moistureType = levelData.environmentData.moistureType;
            environmentData.plantLevelType = levelData.environmentData.plantLevelType;
        }

        
    }

    [Serializable]
    public class EnvironmentData
    {
        public TerrainType terrainType;
        public MoistureType moistureType;
        public PlantLevelType plantLevelType;
    }
        

}