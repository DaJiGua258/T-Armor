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
        public LevelDataModel LoadedLevelData { get; }  // 当前选中的关卡数据，用于后续加载读取（选关界面的数据）
        public void AddLevel(LevelDataModel levelData);
        public void SelectedLevel(PlanetNodeMapData nodeData);
        public MapGeneratorParametersSO GeMapConfigFromLevelData();
    }

    public class LevelSystem : AbstractSystem, ILevelSystem
    {
        public LevelDataModel LoadedLevelData { get; private set; }  // 当前已加载的关卡数据
        private List<LevelDataModel> _levelDataCache = new List<LevelDataModel>();

        

        protected override void OnInit()
        {
            LoadedLevelData = new LevelDataModel();
        }

        public void AddLevel(LevelDataModel levelData)
        {
            _levelDataCache.Add(levelData);
        }

    
        public void SelectedLevel(PlanetNodeMapData nodeData)
        {
            LoadedLevelData.seed.Value = nodeData.Seed;
            LoadedLevelData.environmentData.terrainType = nodeData.environmentData.terrainType;
            LoadedLevelData.environmentData.moistureType = nodeData.environmentData.moistureType;
            LoadedLevelData.environmentData.plantLevelType = nodeData.environmentData.plantLevelType;

            this.SendEvent<UpdateMapInfo>(new UpdateMapInfo());
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