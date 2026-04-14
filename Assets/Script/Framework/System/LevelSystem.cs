using System.Collections.Generic;
using QFramework.Model;
using QFramework.Event;

namespace QFramework.System
{
    public interface ILevelSystem : ISystem
    {
        public LevelDataModel CurrentSelectLevelData { get; }  // 当前选中的关卡数据
        public LevelDataModel LoadedLevelData { get;}  // 当前读取的关卡数据


        public void AddLevel(LevelDataModel levelData);
        public void SelectedLevel(PlanetNodeMapData nodeData);
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
            CurrentSelectLevelData.TerrainTierType = nodeData.TerrainTierType;
            CurrentSelectLevelData.MoistureBandType = nodeData.MoistureBandType;
            CurrentSelectLevelData.PlantLevelType = nodeData.PlantLevelType;

            this.SendEvent<UpdateMapInfo>(new UpdateMapInfo());
        }
    }

    public class LevelDataModel
    {
        public BindableProperty<int> seed = new BindableProperty<int>();  // 地图种子
        public PlanetTerrainType TerrainTierType;  // 地形类型
        public PlanetMoistureType MoistureBandType;  // 湿度类型
        public PlantLevelType PlantLevelType;  // 植物等级类型
        public int LevelIndex = -1;  // 关卡索引

        public LevelDataModel() { }

        public LevelDataModel(int seed, PlanetTerrainType terrainTierType, PlanetMoistureType moistureBandType, PlantLevelType plantLevelType)
        {
            this.seed.Value = seed;
            TerrainTierType = terrainTierType;
            MoistureBandType = moistureBandType;
            PlantLevelType = plantLevelType;
        }

        public void CopyDataFrom(LevelDataModel levelData)
        {
            seed.Value = levelData.seed.Value;
            TerrainTierType = levelData.TerrainTierType;
            MoistureBandType = levelData.MoistureBandType;
            PlantLevelType = levelData.PlantLevelType;
        }
    }
        

}