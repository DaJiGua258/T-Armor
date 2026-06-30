using System.Collections.Generic;
using QFramework.Enum;
using QFramework.System;
using UnityEngine;
using System;

namespace QFramework.Model
{
    /// <summary>
    /// 存档顶层数据
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        public List<CompletedLevelData> CompletedLevels = new();
        public List<PendingNodeData> PendingNodes = new();
    }

    /// <summary>
    /// 已通关关卡数据
    /// </summary>
    [Serializable]
    public class CompletedLevelData
    {
        // ---- 节点位置（核心，用于重建星球上的位置） ----
        public Vector3 SurfaceNormal;

        // ---- 环境数据 ----
        public int Seed;
        public TerrainType terrainType;
        public MoistureType moistureType;
        public PlantLevelType plantLevelType;

        // ---- 关卡配置 ----
        public string LevelName;
        public string LevelDescription;
        public int LevelMissionType;
        public int MissionType;

        // ---- 通关统计 ----
        public float CompletionTimeSeconds;
        public int Kills;
        public int DamageDealt;
        public int DamageTaken;
        public int ShotsFired;
        public int Accuracy; // 0-100 百分比

        public LevelDataModel ToLevelDataModel()
        {
            var model = new LevelDataModel();
            model.seed.Value = Seed;
            model.EnvironmentData.SurfaceNormal = SurfaceNormal;
            model.EnvironmentData.terrainType = terrainType;
            model.EnvironmentData.moistureType = moistureType;
            model.EnvironmentData.plantLevelType = plantLevelType;
            model.LevelMissionConfig.LevelName = LevelName;
            model.LevelMissionConfig.LevelDescription = LevelDescription;
            model.LevelMissionConfig.LevelMissionType = (LevelMissionTypeEnum)LevelMissionType;
            model.LevelMissionConfig.MissionType = (MissionTypeEnum)MissionType;
            return model;
        }
    }

    /// <summary>
    /// 待选（未通关）节点数据
    /// </summary>
    [Serializable]
    public class PendingNodeData
    {
        public Vector3 SurfaceNormal;
        public float HeightNoise;
        public float MoistureNoise;
        public int Seed;
        public bool IsLand;
        public bool IsSunlit;
        public TerrainType terrainType;
        public MoistureType moistureType;
        public PlantLevelType plantLevelType;
    }
}
