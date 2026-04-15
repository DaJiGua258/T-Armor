using System.Collections.Generic;
using UnityEngine;

namespace QFramework.Model
{
    public interface ILevelTypeModel : IModel
    {
        public string GetTerrainTypeName(TerrainType type);
        public string GetPlantLevelTypeName(PlantLevelType type);
        public string GetMoistureTypeName(MoistureType type);
    }

    public class LevelTypeModel : AbstractModel, ILevelTypeModel
    {
        // ----- 维护的信息 -------------------------
        private Dictionary<TerrainType, string> _terrainTypeNames = new Dictionary<TerrainType, string>()
        {
            {TerrainType.Ocean, "海洋"},
            {TerrainType.Shore, "海岸"},
            {TerrainType.Plain, "平原"},
            {TerrainType.Hills, "丘陵"},
            {TerrainType.Highlands, "高地"},
            {TerrainType.Valleys, "山谷"},
            {TerrainType.Snow, "雪地"},
            {TerrainType.Polar, "极地"},
        };

        private Dictionary<PlantLevelType, string> _plantTypeNames = new Dictionary<PlantLevelType, string>()
        {
            {PlantLevelType.Sparse, "稀疏"},
            {PlantLevelType.Regular, "普通"},
            {PlantLevelType.Dense, "密集"},
        };

        private Dictionary<MoistureType, string> _moistureTypeNames = new Dictionary<MoistureType, string>()
        {
            {MoistureType.Dry, "干燥"},
            {MoistureType.Wet, "湿润"},
        };

        protected override void OnInit()
        {
           
        }

        public string GetMoistureTypeName(MoistureType type)
        {
            return _moistureTypeNames[type];
        }

        public string GetPlantLevelTypeName(PlantLevelType type)
        {
            return _plantTypeNames[type];
        }

        public string GetTerrainTypeName(TerrainType type)
        {
            return _terrainTypeNames[type];
        }
    }

    public enum TerrainType
    {
        Ocean,     // 海洋
        Shore,     // 海岸（seaLevel ~ shoreThreshold 之间的沙滩带）
        Plain,    // 平原
        Hills,    // 丘陵
        Highlands, // 高地
        Valleys, // 山谷
        Snow,      // 雪地
        Polar,     // 极地
    }

    // 湿度类型
    public enum MoistureType
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