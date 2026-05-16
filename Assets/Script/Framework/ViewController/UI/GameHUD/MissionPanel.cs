using System;
using System.Collections;
using System.Collections.Generic;
using QFramework.Command;
using QFramework.Enum;
using QFramework.Model;
using QFramework.System;
using QFramework.UtilityKit;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class MissionPanel : BaseUIComponent
    {
        private const int PrimaryMissionIndex = 1;

        [SerializeField] private GameObject _pf_priItem;
        [SerializeField] private MissionItem _primaryMissionItem;

        [SerializeField] private GameObject _pf_preItem;
        [SerializeField] private List<MissionItem> _preMissionItems = new();
        

        void Start()
        {
            // 初始化主要任务和前置任务
            InitPrimaryItem();
            InitPreItem();

            // 刷新布局
            StartCoroutine(RefreshLayOut());
        }

        void OnEnable()
        {
            StartCoroutine(RefreshLayOut());
        }

        void Update()
        {

        }

        IEnumerator RefreshLayOut()
        {
            Canvas.ForceUpdateCanvases();
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        }

        private void RegisterMissionEvent(MissionItem item, MissionDataModel mission)
        {
            // 任务【阶段索引】变化事件
            mission.StepIndex.Register(value => UpdateInfo(item, mission))
                .UnRegisterWhenGameObjectDestroyed(item.Item.gameObject);

            // 任务【阶段进度】变化事件
            foreach(var step in mission.StepList)
            {
                step.Register(value => UpdateInfo(item, mission))
                    .UnRegisterWhenGameObjectDestroyed(item.Item.gameObject);
            }

            // 任务状态变化事件
            mission.MissionState.Register(value => UpdateByState(item, value, mission))
                .UnRegisterWhenGameObjectDestroyed(item.Item.gameObject);

            // ----- 初始化更新一次任务信息 -------------------------
            UpdateInfo(item, mission);
            UpdateByState(item, mission.MissionState.Value, mission);
        }

        private void InitPrimaryItem()
        {
            if(MissionSystem.Missions.Count <= PrimaryMissionIndex)
            {
                return;
            }

            var priMission = MissionSystem.Missions[PrimaryMissionIndex];
            if(priMission.MissionType == MissionTypeEnum.None)
            {
                return;
            }

            var missionObj = Instantiate(_pf_priItem, transform);


            _primaryMissionItem = new MissionItem(missionObj.transform);
            var mission = priMission;

            // ----- 注册与初始化任务事件 -------------------------
            RegisterMissionEvent(_primaryMissionItem, mission);
        }

        public void InitPreItem()
        {
            int missionCount = MissionSystem.Missions.Count;

            // 遍历初始化前置任务（跳过 Entry[0]、Primary[1]、Extraction[last]）
            for(int missionIndex = 2; missionIndex < missionCount - 1; missionIndex++)
            {
                var mission = MissionSystem.Missions[missionIndex];
                if(mission.MissionType == MissionTypeEnum.None)
                {
                    continue;
                }

                var missionObj = Instantiate(_pf_preItem, transform);
                var preItem = new MissionItem(missionObj.transform);
                _preMissionItems.Add(preItem);

                // ----- 注册与初始化任务事件 -------------------------
                RegisterMissionEvent(preItem, mission);
            }
        }

        private void UpdateInfo(MissionItem item, MissionDataModel mission)
        {
            if(mission.StepIndex.Value > mission.MissionConfig.MissionSteps.Length - 1)
            {
                return;
            }

            item.TipText.text = "";

            item.NameText.text = "// " + mission.MissionConfig.MissionName;
            var steps = mission.MissionConfig.MissionSteps;

            // var tip = steps[mission.StepIndex.Value].TipText;
            // var step = steps[mission.StepIndex.Value].Progress;  // 总步骤
            // var progress = mission.StepList[mission.StepIndex.Value].Value;  // 当前步骤进度
            
            for(int i = 0; i <= mission.StepIndex.Value; i++)
            {
                // 读取目标任务步骤信息
                var tip = steps[i].TipText;
                var progress = steps[i].Progress;

                // 获取当前进度
                var curProgress = mission.StepList[i].Value;

                item.TipText.text += $"  > {tip} ({curProgress}/{progress})";

                if(i + 1 <= mission.StepIndex.Value)
                {
                    item.TipText.text += "\n";
                }
            }

            if (gameObject.activeInHierarchy)
                StartCoroutine(RefreshLayOut());
        }



        private void UpdateByState(MissionItem item, MissionState state, MissionDataModel mission)
        {
            if(state == MissionState.Completed)
            {
                item.TipText.text = "  > 已完成";
            }
            else if(state == MissionState.InProgress)
            {
                item.TipText.transform.parent.gameObject.SetActive(true);
                UpdateInfo(item, mission); // 更新任务信息，否则数据不变化，UI不刷新；
            }
            else if(state == MissionState.NotStarted)
            {
                item.TipText.transform.parent.gameObject.SetActive(false);
            }
            else if(state == MissionState.Pause)
            {
                item.TipText.transform.parent.gameObject.SetActive(true);
                item.TipText.text = "  > 返回任务地点";
            }

            if (gameObject.activeInHierarchy)
                StartCoroutine(RefreshLayOut());
        }
    }

    [Serializable]
    public class MissionItem
    {
        public RectTransform Item;
        public Image TileImg;
        public Text NameText;
        public Text TipText;
        
        

        public MissionItem(Transform parent)
        {
            Item = parent.GetComponent<RectTransform>();
            TileImg = parent.Find("Title").GetComponent<Image>();
            NameText = parent.Find("Title/Txt").GetComponent<Text>();
            TipText = parent.Find("Tip/Txt").GetComponent<Text>();
        }
    }
}