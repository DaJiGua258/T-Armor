using System.Collections.Generic;
using QFramework.Enum;
using UnityEngine;

namespace QFramework.Model
{
    public interface IMissionConfigModel : IModel
    {
        public MissionConfig GetConfig(MissionTypeEnum missionType);
    }

    public class MissionConfigModel : AbstractSystem, IMissionConfigModel
    {
        

        public Dictionary<MissionTypeEnum, MissionConfig> MissionConfigCache = new Dictionary<MissionTypeEnum, MissionConfig>()
        {
            // 任务1
            {MissionTypeEnum.Mission_1, new MissionConfig(
                MissionTypeEnum.Mission_1,
                null,
                "Mission_1",
                new string[] { "Mission_1" },
                "Mission_1",
                null)},

            // 任务2
            {MissionTypeEnum.Mission_2, new MissionConfig(
                MissionTypeEnum.Mission_2,
                null,
                "Mission_2",
                new string[] { "Mission_2" },
                "Mission_2",
                null)},

            // 任务3
            {MissionTypeEnum.Mission_3, new MissionConfig(
                MissionTypeEnum.Mission_3,
                null,
                "Mission_3",
                new string[] { "Mission_3" },
                "Mission_3",
                null)},
        };

        protected override void OnInit()
        {
            
        }

        public MissionConfig GetConfig(MissionTypeEnum missionType)
        {
            return MissionConfigCache[missionType];
        }

    }

    public class MissionConfig
    {
        // 任务标识
        public MissionTypeEnum MissionType;

        // 任务信息
        public Sprite MissionIcon;
        public string MissionName;  // 任务名称
        public string[] TipText;  // 任务提示文本
        public string MissionDescription;  // 任务描述

        // 任务实例对象（生成玩家可交互的游戏物体，如建筑等）
        public GameObject MissionInstanceObject;

        public MissionConfig(
            MissionTypeEnum missionType, 
            Sprite missionIcon, 
            string missionName, 
            string[] tipText, 
            string missionDescription, 
            GameObject missionInstanceObject)
        {
            MissionType = missionType;
            MissionIcon = missionIcon;
            MissionName = missionName;
            TipText = tipText;
            MissionDescription = missionDescription;
            MissionInstanceObject = missionInstanceObject;
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