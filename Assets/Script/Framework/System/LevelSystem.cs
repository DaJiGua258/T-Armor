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
        public void SavePlayerLoadoutToDisk();  // 保存玩家配装到存档
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

            // 从存档恢复已完成关卡 & 玩家配装
            LoadProgress();
            RestorePlayerLoadoutFromSave();
        }

        public void SaveLevelProgress()
        {
            if (_levelAlreadySaved) return;
            _levelAlreadySaved = true;

            // 将当前统计数据写入 LoadedLevelData，确保内存缓存中的数据也是完整的
            var stats = this.GetSystem<IStatsSystem>();
            LoadedLevelData.CompletionTimeSeconds = stats.GameTimeSeconds;
            LoadedLevelData.Kills = stats.TotalKills;
            LoadedLevelData.DamageDealt = stats.TotalDamageDealt;
            LoadedLevelData.DamageTaken = stats.TotalDamageTaken;
            LoadedLevelData.ShotsFired = stats.TotalShotsFired;
            LoadedLevelData.Accuracy = Mathf.RoundToInt(stats.GetAccuracy() * 100f);

            LevelDataCache.Add(LoadedLevelData);
            SaveProgress();
        }

        private bool _levelAlreadySaved;

        public void AddLoadLevel()
        {
            if (!_levelAlreadySaved)
            {
                // 确保内存缓存中的统计数据也是最新的
                var stats = this.GetSystem<IStatsSystem>();
                LoadedLevelData.CompletionTimeSeconds = stats.GameTimeSeconds;
                LoadedLevelData.Kills = stats.TotalKills;
                LoadedLevelData.DamageDealt = stats.TotalDamageDealt;
                LoadedLevelData.DamageTaken = stats.TotalDamageTaken;
                LoadedLevelData.ShotsFired = stats.TotalShotsFired;
                LoadedLevelData.Accuracy = Mathf.RoundToInt(stats.GetAccuracy() * 100f);

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

            // 保存玩家配装
            SavePlayerLoadout(saveData);

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

        /// <summary>
        /// 从 UI（配置面板等）触发玩家配装存盘，不影响关卡进度
        /// </summary>
        public void SavePlayerLoadoutToDisk()
        {
            var saveData = _storage.LoadData<GameSaveData>(SAVE_KEY) ?? new GameSaveData();
            // 保留已有的关卡进度
            if (saveData.CompletedLevels.Count == 0 && LevelDataCache.Count > 0)
            {
                foreach (var level in LevelDataCache)
                {
                    var env = level.EnvironmentData;
                    var config = level.LevelMissionConfig;
                    saveData.CompletedLevels.Add(new CompletedLevelData
                    {
                        SurfaceNormal = env.SurfaceNormal,
                        Seed = level.seed.Value,
                        terrainType = env.terrainType,
                    });
                }
            }
            SavePlayerLoadout(saveData);
            _storage.SaveData(SAVE_KEY, saveData);
        }

        // ===== 玩家配装 存档 / 读档 ================================

        private IPlayerSystem _playerLoadoutCache => this.GetSystem<IPlayerSystem>();
        private IInvenotrySystem _inventoryCache => this.GetSystem<IInvenotrySystem>();
        private IWeaponConfigModel _weaponConfigCache => this.GetModel<IWeaponConfigModel>();
        private IItemConfigModel _itemConfigCache => this.GetModel<IItemConfigModel>();
        private IModConfigModel _modConfigCache => this.GetModel<IModConfigModel>();

        public void SavePlayerLoadout(GameSaveData saveData)
        {
            var loadout = new PlayerLoadoutData();

            // 支援物品
            foreach (var item in _playerLoadoutCache.SupportItems)
                loadout.SupportItems.Add(item.SupportType);

            // 武器 4 槽
            SaveWeaponSlot(_playerLoadoutCache.PlayerWeapon.Left.Value, loadout.WeaponLeft);
            SaveWeaponSlot(_playerLoadoutCache.PlayerWeapon.Right.Value, loadout.WeaponRight);
            SaveWeaponSlot(_playerLoadoutCache.PlayerWeapon.HangerLeft.Value, loadout.WeaponHangerLeft);
            SaveWeaponSlot(_playerLoadoutCache.PlayerWeapon.HangerRight.Value, loadout.WeaponHangerRight);

            // 背包
            foreach (var item in _inventoryCache.ItemDataCache)
            {
                var slot = new InventorySlotSaveData
                {
                    ItemType = item.ItemType,
                    Count = item.Count.Value,
                };
                if (item.ModData != null)
                {
                    foreach (var entry in item.ModData.Entries)
                        slot.ModEntries.Add(new ModEntrySaveData
                        {
                            Target = entry.Target,
                            Value = entry.Value,
                            Operator = entry.Operator,
                        });
                }
                loadout.Inventory.Add(slot);
            }

            saveData.PlayerLoadout = loadout;
        }

        public void RestorePlayerLoadoutFromSave()
        {
            var saveData = _storage.LoadData<GameSaveData>(SAVE_KEY);
            if (saveData?.PlayerLoadout == null) return;
            RestorePlayerLoadout(saveData.PlayerLoadout);
        }

        public void RestorePlayerLoadout(PlayerLoadoutData loadout)
        {
            if (loadout == null) return;

            // 支援物品
            if (loadout.SupportItems.Count > 0)
                _playerLoadoutCache.InitSupportItems(loadout.SupportItems);

            // 武器 4 槽 — 直接还原到对应槽位
            var pw = _playerLoadoutCache.PlayerWeapon;
            var restored = RestoreWeaponData(loadout.WeaponLeft, false);
            if (restored != null) pw.Left.Value = restored;
            restored = RestoreWeaponData(loadout.WeaponRight, false);
            if (restored != null) pw.Right.Value = restored;
            restored = RestoreWeaponData(loadout.WeaponHangerLeft, true);
            if (restored != null) pw.HangerLeft.Value = restored;
            restored = RestoreWeaponData(loadout.WeaponHangerRight, true);
            if (restored != null) pw.HangerRight.Value = restored;

            // 背包
            for (int i = 0; i < loadout.Inventory.Count && i < _inventoryCache.ItemDataCache.Count; i++)
            {
                var slotSave = loadout.Inventory[i];
                var slot = _inventoryCache.ItemDataCache[i];

                slot.CopyFrom(new ItemDataModel(_itemConfigCache.GetItemConfig(ItemTypeEnum.None)));

                if (slotSave.ItemType == ItemTypeEnum.None) continue;

                var itemCfg = _itemConfigCache.GetItemConfig(slotSave.ItemType);
                if (itemCfg == null) continue;

                slot.CopyFrom(new ItemDataModel(itemCfg));
                slot.Count.Value = slotSave.Count;

                if (slotSave.ModEntries.Count > 0)
                {
                    var entries = new List<ModEntry>();
                    foreach (var me in slotSave.ModEntries)
                        entries.Add(new ModEntry(me.Target, me.Value, me.Operator));
                    slot.ModData = new ModData(slotSave.ItemType, entries);
                }
            }
        }

        private void SaveWeaponSlot(WeaponDataModel weaponData, WeaponSlotSaveData slotSave)
        {
            if (weaponData == null || weaponData.WeaponType == WeaponTypeEnum.None) return;
            slotSave.WeaponType = weaponData.WeaponType;

            foreach (var mod in weaponData.EquippedMods)
            {
                if (mod == null || mod.ItemType == ItemTypeEnum.None) continue;
                var modSave = new ItemSaveData
                {
                    ItemType = mod.ItemType,
                    Count = mod.Count.Value,
                };
                if (mod.ModData != null)
                {
                    foreach (var entry in mod.ModData.Entries)
                        modSave.ModEntries.Add(new ModEntrySaveData
                        {
                            Target = entry.Target,
                            Value = entry.Value,
                            Operator = entry.Operator,
                        });
                }
                slotSave.EquippedMods.Add(modSave);
            }
        }

        private WeaponDataModel RestoreWeaponData(WeaponSlotSaveData slotSave, bool isHanger)
        {
            if (slotSave == null || slotSave.WeaponType == WeaponTypeEnum.None) return null;

            var config = isHanger
                ? _weaponConfigCache.GetHangerWeaponConfigModel(slotSave.WeaponType)
                : _weaponConfigCache.GetWeaponConfigModel(slotSave.WeaponType);

            if (config == null) return null;

            var weaponData = new WeaponDataModel(config);

            foreach (var modSave in slotSave.EquippedMods)
            {
                if (modSave.ItemType == ItemTypeEnum.None) continue;

                var itemData = new ItemDataModel(_itemConfigCache.GetItemConfig(modSave.ItemType));
                itemData.Count.Value = modSave.Count;

                if (modSave.ModEntries.Count > 0)
                {
                    var entries = new List<ModEntry>();
                    foreach (var me in modSave.ModEntries)
                        entries.Add(new ModEntry(me.Target, me.Value, me.Operator));
                    itemData.ModData = new ModData(modSave.ItemType, entries);
                }

                for (int m = 0; m < weaponData.EquippedMods.Count; m++)
                {
                    if (weaponData.EquippedMods[m].ItemType == ItemTypeEnum.None)
                    {
                        weaponData.EquippedMods[m] = itemData;
                        break;
                    }
                }
            }

            weaponData.RecalculateStats();
            return weaponData;
        }
    }

    public class LevelDataModel
    {
        // ----- 关卡信息 -------------------------
        public LevelMissionConfig LevelMissionConfig = new LevelMissionConfig();

        // ----- 地图信息 -------------------------
        public BindableProperty<int> seed = new BindableProperty<int>();  // 地图种子
        public EnvironmentData EnvironmentData = new EnvironmentData();

        // ----- 通关统计（仅在已通关关卡中有值） ----
        public float CompletionTimeSeconds;
        public int Kills;
        public int DamageDealt;
        public int DamageTaken;
        public int ShotsFired;
        public int Accuracy;
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