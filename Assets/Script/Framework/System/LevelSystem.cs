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
        public void SaveLevelProgress();
    }

    public class LevelSystem : AbstractSystem, ILevelSystem
    {
        private IMissionSystem _missionSystem => this.GetSystem<IMissionSystem>();
        private IMissionConfigModel _missionConfigModel => this.GetModel<IMissionConfigModel>();
        private IStorageUtility _storage => this.GetUtility<IStorageUtility>();
        // ----- 已经通过的关卡缓存 -------------------------
        public List<LevelDataModel> LevelDataCache { get; private set; } = new List<LevelDataModel>();

        // ----- 当前选中的关卡数据 -------------------------
        public LevelDataModel LoadedLevelData { get; private set; }


        protected override void OnInit()
        {
            LoadedLevelData = new LevelDataModel();
            _missionSystem.InitMission(LevelMissionTypeEnum.LevMis_Beacon);

            // 从存档恢复已完成关卡
            LoadProgress();
        }

        public void SaveLevelProgress()
        {
            if (_levelAlreadySaved) return;
            _levelAlreadySaved = true;
            LevelDataCache.Add(LoadedLevelData);
            SaveProgress();
        }

        private bool _levelAlreadySaved;

        public void AddLoadLevel()
        {
            if (!_levelAlreadySaved)
            {
                LevelDataCache.Add(LoadedLevelData);
            }
            _levelAlreadySaved = false;

            // 更新最终统计数据（包含撤离阶段的变动）
            SaveProgress();

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

            // 直接使用信标任务类型，不再随机选择
            var levelConfig = _missionConfigModel.GetLevelConfig(LevelMissionTypeEnum.LevMis_Beacon);
            LoadedLevelData.LevelMissionConfig.LevelName = levelConfig.LevelName;
            LoadedLevelData.LevelMissionConfig.LevelDescription = levelConfig.LevelDescription;
            LoadedLevelData.LevelMissionConfig.LevelMissionType = levelConfig.LevelMissionType;
            LoadedLevelData.LevelMissionConfig.MissionType = levelConfig.MissionType;

            this.SendEvent<UpdateMapInfo>(new UpdateMapInfo());
        }

        // ----- 存档 / 读档 ----------------------------------------
        private const string SAVE_KEY = "GameSaveData";

        public void SaveProgress()
        {
            var stats = this.GetSystem<IStatsSystem>();

            // 读取已有存档，保留旧关卡的统计数据
            var existingSave = _storage.LoadData<GameSaveData>(SAVE_KEY);
            var saveData = new GameSaveData();

            for (int i = 0; i < LevelDataCache.Count; i++)
            {
                var level = LevelDataCache[i];
                var env = level.EnvironmentData;
                var config = level.LevelMissionConfig;

                var data = new CompletedLevelData
                {
                    SurfaceNormal = env.SurfaceNormal,
                    Seed = level.seed.Value,
                    terrainType = env.terrainType,
                    moistureType = env.moistureType,
                    plantLevelType = env.plantLevelType,
                    LevelName = config.LevelName,
                    LevelDescription = config.LevelDescription,
                    LevelMissionType = (int)config.LevelMissionType,
                    MissionType = (int)config.MissionType,
                };

                // 恢复旧关卡的先前统计数据
                if (existingSave != null && i < existingSave.CompletedLevels.Count)
                {
                    var old = existingSave.CompletedLevels[i];
                    data.CompletionTimeSeconds = old.CompletionTimeSeconds;
                    data.Kills = old.Kills;
                    data.DamageDealt = old.DamageDealt;
                    data.DamageTaken = old.DamageTaken;
                    data.ShotsFired = old.ShotsFired;
                    data.Accuracy = old.Accuracy;
                }

                // 最新关卡用当前统计数据（已在每局开始由 ResetSessionStats 清过零）
                if (i == LevelDataCache.Count - 1)
                {
                    data.CompletionTimeSeconds = stats.GameTimeSeconds;
                    data.Kills = stats.TotalKills;
                    data.DamageDealt = stats.TotalDamageDealt;
                    data.DamageTaken = stats.TotalDamageTaken;
                    data.ShotsFired = stats.TotalShotsFired;
                    data.Accuracy = Mathf.RoundToInt(stats.GetAccuracy() * 100f);
                }

                saveData.CompletedLevels.Add(data);
            }

            _storage.SaveData(SAVE_KEY, saveData);
        }

        public void LoadProgress()
        {
            var saveData = _storage.LoadData<GameSaveData>(SAVE_KEY);
            if (saveData == null || saveData.CompletedLevels.Count == 0) return;

            LevelDataCache.Clear();
            foreach (var data in saveData.CompletedLevels)
            {
                LevelDataCache.Add(data.ToLevelDataModel());
            }
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