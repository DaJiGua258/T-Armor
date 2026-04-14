using System.Collections.Generic;

namespace QFramework.Model
{
    public interface ILevelTypeModel : IModel
    {
        public string GetTerrainTypeName(PlanetTerrainType type);
        public string GetPlantLevelTypeName(PlantLevelType type);
        public string GetMoistureTypeName(PlanetMoistureType type);
    }

    public class LevelTypeModel : AbstractModel, ILevelTypeModel
    {
        // ----- 维护的信息 -------------------------
        private Dictionary<PlanetTerrainType, string> _terrainTypeNames = new Dictionary<PlanetTerrainType, string>()
        {
            {PlanetTerrainType.Ocean, "海洋"},
            {PlanetTerrainType.Shore, "海岸"},
            {PlanetTerrainType.Plain1, "平原1"},
            {PlanetTerrainType.Plain2, "平原2"},
            {PlanetTerrainType.Mountain1, "山脉1"},
            {PlanetTerrainType.Mountain2, "山脉2"},
            {PlanetTerrainType.Snow, "雪地"},
            {PlanetTerrainType.Polar, "极地"},
        };

        private Dictionary<PlantLevelType, string> _plantTypeNames = new Dictionary<PlantLevelType, string>()
        {
            {PlantLevelType.Sparse, "稀疏"},
            {PlantLevelType.Regular, "普通"},
            {PlantLevelType.Dense, "密集"},
        };

        private Dictionary<PlanetMoistureType, string> _moistureTypeNames = new Dictionary<PlanetMoistureType, string>()
        {
            {PlanetMoistureType.Dry, "干燥"},
            {PlanetMoistureType.Wet, "湿润"},
        };

        protected override void OnInit()
        {
           
        }

        public string GetMoistureTypeName(PlanetMoistureType type)
        {
            return _moistureTypeNames[type];
        }

        public string GetPlantLevelTypeName(PlantLevelType type)
        {
            return _plantTypeNames[type];
        }

        public string GetTerrainTypeName(PlanetTerrainType type)
        {
            return _terrainTypeNames[type];
        }

        
    }

    public enum PlanetTerrainType
    {
        Ocean,     // 海洋
        Shore,     // 海岸（seaLevel ~ shoreThreshold 之间的沙滩带）
        Plain1,    // 平原类型1
        Plain2,    // 平原类型2
        Mountain1, // 山脉类型1
        Mountain2, // 山脉类型2
        Snow,      // 雪地
        Polar,     // 极地
    }

    // 湿度类型
    public enum PlanetMoistureType
    {
        Dry,
        Wet,
    }

    // 植物等级类型
    public enum PlantLevelType
    {
        Sparse,  // 稀疏
        Regular,  // 常规
        Dense,  // 茂密
    }
}