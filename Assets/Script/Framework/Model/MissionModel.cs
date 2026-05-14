using System.Collections.Generic;
using System.Linq;
using QFramework.Enum;
using UnityEngine;

namespace QFramework.Model
{
    public interface IMissionConfigModel : IModel
    {
        public LevelMissionConfig GetLevelConfig(LevelMissionTypeEnum levelMissionType);
        public MissionConfig GetConfig(MissionTypeEnum missionType);
        public List<LevelMissionTypeEnum> GetAvailableLevelMissionTypes();
    }

    public class MissionConfigModel : AbstractModel, IMissionConfigModel
    {
        public Dictionary<LevelMissionTypeEnum, LevelMissionConfig> LevelMissionConfigCache = new()
        {
            {
                LevelMissionTypeEnum.LevMis_CleaArea,  // 保存关卡信息
                new LevelMissionConfig(
                    new List<MissionTypeEnum>
                    {
                        // 主任务（None 表示当前关卡无主任务）
                        MissionTypeEnum.Pre_EnemyKill,

                        // 前置任务
                        MissionTypeEnum.Pre_EnemyKill,
                        MissionTypeEnum.Pre_EnemyKill
                    }
                )
                {
                    LevelName = "清空区域",
                    LevelDescription = "清除指定区域内的所有敌人",
                    LevelMissionType = LevelMissionTypeEnum.LevMis_CleaArea
                }
            },

        };

        public Dictionary<MissionTypeEnum, MissionConfig> MissionConfigCache = new Dictionary<MissionTypeEnum, MissionConfig>()
        {
            // 占位符
            {
                MissionTypeEnum.None, new MissionConfig(
                    MissionTypeEnum.None,
                    null,
                    "None",
                    new MissionStep[] { },
                    "None",
                    null
                )
            },

            // 进入任务（玩家出生点）
            {
                MissionTypeEnum.Entry, new MissionConfig(
                    MissionTypeEnum.Entry,
                    null,
                    "进入任务",
                    new MissionStep[] { },
                    "进入任务",
                    null
                )
            },

            // 任务1
            {
                MissionTypeEnum.Pre_EnemyKill, new MissionConfig(
                    MissionTypeEnum.Pre_EnemyKill,
                    null,
                    "消灭敌人",
                    new MissionStep[] { new MissionStep("在当前区域消灭敌人", 20) },
                    "Mission_1_description",
                    null
                )
            },

            // 任务2
            {MissionTypeEnum.Mission_2, new MissionConfig(
                MissionTypeEnum.Mission_2,
                null,
                "Mission_2",
                new MissionStep[] { new MissionStep("Mission_2_tip_1", 1), new MissionStep("Mission_2_tip_2", 2), new MissionStep("Mission_2_tip_3", 3) },
                "Mission_2_description",
                null)},

            // 任务3
            {MissionTypeEnum.Mission_3, new MissionConfig(
                MissionTypeEnum.Mission_3,
                null,
                "Mission_3",
                new MissionStep[] { new MissionStep("Mission_3_tip_1", 1), new MissionStep("Mission_3_tip_2", 2), new MissionStep("Mission_3_tip_3", 3) },
                "Mission_3_description",
                null)},
            
            // 任务4
            {MissionTypeEnum.Mission_4, new MissionConfig(
                MissionTypeEnum.Mission_4,
                null,
                "Mission_4",
                new MissionStep[] { new MissionStep("Mission_4", 1) },
                "Mission_4",
                null)},

            // 撤离任务
            {MissionTypeEnum.Extraction, new MissionConfig(
                MissionTypeEnum.Extraction,
                null,
                "撤离任务",
                new MissionStep[] { new MissionStep("到达撤离点", 1) },
                "撤离任务",
                null)},
        };

        protected override void OnInit()
        {
            
        }

        public MissionConfig GetConfig(MissionTypeEnum missionType)
        {
            return MissionConfigCache[missionType];
        }

        public LevelMissionConfig GetLevelConfig(LevelMissionTypeEnum levelMissionType)
        {
            return LevelMissionConfigCache[levelMissionType];
        }

        public List<LevelMissionTypeEnum> GetAvailableLevelMissionTypes()
        {
            return LevelMissionConfigCache.Keys.ToList();
        }
    }

    public class MissionConfig
    {
        // 任务标识
        public MissionTypeEnum MissionType;

        // 任务信息
        public Sprite MissionIcon;
        public string MissionName;  // 任务名称
        public MissionStep[] MissionSteps;
        public string MissionDescription;  // 任务描述

        // 任务实例对象（生成玩家可交互的游戏物体，如建筑等）
        public string PrefabPath;

        public MissionConfig(
            MissionTypeEnum missionType, 
            Sprite missionIcon, 
            string missionName, 
            MissionStep[] missionSteps, 
            string missionDescription, 
            string prefabPath)
        {
            MissionType = missionType;
            MissionIcon = missionIcon;
            MissionName = missionName;
            // TipText = tipText;
            MissionSteps = missionSteps;
            MissionDescription = missionDescription;
            PrefabPath = "Prefab/Mission/" + missionType.ToString();
        }
    }
    public struct MissionStep
    {
        public string TipText;  // 任务提示文本

        public int Progress;  // 

        public MissionStep(string tipText, int step)
        {
            TipText = tipText;
            this.Progress = step;
        }
    }

    public class LevelMissionConfig
    {
        // ----- 关卡信息 -------------------------
        public string LevelName;  // 关卡名称
        public string LevelDescription;  // 关卡描述
        public LevelMissionTypeEnum LevelMissionType = LevelMissionTypeEnum.None;  // 关卡任务类型

        // ----- 关卡任务列表 -------------------------
        public List<MissionTypeEnum> MissionTypes = new();

        public LevelMissionConfig() { }

        public LevelMissionConfig(
            List<MissionTypeEnum> missionTypes)
        {
            MissionTypes = missionTypes;
        }
    }

    public enum MissionState
    {
        None,
        NotStarted,  // 任务未开始（主要针对首要任务）
        InProgress,
        Pause,  // 任务暂停（如未在任务范围内）
        Completed,
        Failed,
    }

    public static class MissionTypeHelper
    {
        public static bool IsGameplayMission(MissionTypeEnum type) =>
            type != MissionTypeEnum.Entry &&
            type != MissionTypeEnum.Extraction &&
            type != MissionTypeEnum.None;
    }




}