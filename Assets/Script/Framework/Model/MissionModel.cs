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
                LevelMissionTypeEnum.LevelMission_1,  // 保存关卡信息
                new(

                    MissionTypeEnum.Mission_1, // 主要任务
                    new() { 

                        // 前置任务
                        MissionTypeEnum.Mission_2,  
                        MissionTypeEnum.Mission_3 
                        }
                )
            },

        };

        public Dictionary<MissionTypeEnum, MissionConfig> MissionConfigCache = new Dictionary<MissionTypeEnum, MissionConfig>()
        {
            // 任务1
            {MissionTypeEnum.Mission_1, new MissionConfig(
                MissionTypeEnum.Mission_1,
                null,
                "Mission_1",
                    new MissionStep[] 
                    {
                         new MissionStep("Mission_1_tip_1", 1), 
                         new MissionStep("Mission_1_tip_2", 5), 
                         new MissionStep("Mission_1_tip_3", 2) 
                    },
                "Mission_1_description",
                null)},

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
        public MissionTypeEnum PrimaryMissionType;
        public List<MissionTypeEnum> PrerequiredMissionTypes = new();

        public LevelMissionConfig(
            MissionTypeEnum primaryMissionType,
            List<MissionTypeEnum> prerequiredMissionTypes)
        {
            PrimaryMissionType = primaryMissionType;
            PrerequiredMissionTypes = prerequiredMissionTypes;
        }
    }   

    public enum MissionState
    {
        None,
        NotStarted,
        InProgress,
        Completed,
        Failed,
    }




}