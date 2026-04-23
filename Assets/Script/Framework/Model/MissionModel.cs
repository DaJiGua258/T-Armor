using System.Collections.Generic;
using QFramework.Enum;
using UnityEngine;

namespace QFramework.Model
{
    public interface IMissionConfigModel : IModel
    {
        public LevelMissionConfig GetLevelConfig(LevelMissionTypeEnum levelMissionType);
        public MissionConfig GetConfig(MissionTypeEnum missionType);
    }

    public class MissionConfigModel : AbstractModel, IMissionConfigModel
    {
        public Dictionary<LevelMissionTypeEnum, LevelMissionConfig> LevelMissionConfigCache = new()
        {
            {
                LevelMissionTypeEnum.LevMis_CleaArea,  // 保存关卡信息
                new(
                    new()
                    {
                        // 主任务（None 表示当前关卡无主任务）
                        MissionTypeEnum.Pre_EnemyKill,

                        // 前置任务
                        MissionTypeEnum.Pre_EnemyKill,
                        MissionTypeEnum.Pre_EnemyKill
                    }
                )
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
        public GameObject MissionInstanceObject;

        public MissionConfig(
            MissionTypeEnum missionType, 
            Sprite missionIcon, 
            string missionName, 
            MissionStep[] missionSteps, 
            string missionDescription, 
            GameObject missionInstanceObject)
        {
            MissionType = missionType;
            MissionIcon = missionIcon;
            MissionName = missionName;
            // TipText = tipText;
            MissionSteps = missionSteps;
            MissionDescription = missionDescription;
            MissionInstanceObject = missionInstanceObject;
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
        public List<MissionTypeEnum> MissionTypes = new();

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




}