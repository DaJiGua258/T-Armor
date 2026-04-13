

using System.Collections.Generic;

namespace QFramework.System
{
    public interface ILevelSystem : ISystem
    {
        public void LoadLevel(int index);
        public void AddLevel(LevelDataModel levelData);
    }

    public class LevelSystem : AbstractSystem, ILevelSystem
    {
        public LevelDataModel CurrentLevelData { get; private set; }
        private List<LevelDataModel> _levelDataCache = new List<LevelDataModel>();
        public void LoadLevel(int index)
        {
            
        }

        public void AddLevel(LevelDataModel levelData)
        {
            _levelDataCache.Add(levelData);
        }

        protected override void OnInit()
        {
            
        }

        
    }

    public class LevelDataModel
    {
        public int seed;
        public PlanetTerrainType TerrainTierType;
        public PlanetMoistureType MoistureBandType;
        public int LevelIndex;
    }

        public enum PlanetTerrainType
        {
            Ocean,  // 海洋
            Plain1,  // 平原类型1
            Plain2,  // 平原类型2
            Mountain1,  // 山脉类型1
            Mountain2,  // 山脉类型2
            Snow,  // 雪地
            Polar,  // 极地
        }

        public enum PlanetMoistureType
        {
            Dry,
            Wet,
        }

}