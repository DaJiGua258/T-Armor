using System;
using System.Collections;
using System.Collections.Generic;
using QFramework.Command;
using QFramework.Model;
using QFramework.System;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class MissionPanel : AbstractBasePanel
    {
        [SerializeField] private GameObject _pf_priItem;
        [SerializeField] private MissionItem _primaryMissionItem;

        [SerializeField] private GameObject _pf_preItem;
        [SerializeField] private List<MissionItem> _preMissionItems = new();
        

        void Start()
        {
            InitPrimaryItem();
            // InitPreItem();

            
            StartCoroutine(RefreshLayOut());
        }

        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                this.SendCommand(new MissionCommand.Add(1));
            }
        }

        IEnumerator RefreshLayOut()
        {
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        }

        private void RegisterMissionEvent(MissionDataModel mission )
        {
            mission.StepIndex.Register(value => UpdateInfo(_primaryMissionItem, mission));
            foreach(var step in mission.StepList)
            {
                step.Register(value => UpdateInfo(_primaryMissionItem, mission));
            }

            mission.MissionState.Register(value => 
            {
                if(value == MissionState.Completed)
                {
                    UpdateFinishedInfo(_primaryMissionItem);
                }
            });
        }

        private void InitPrimaryItem()
        {
            var missionObj = Instantiate(_pf_priItem, transform);


            _primaryMissionItem = new MissionItem(missionObj.transform);
            var mission = MissionSystem.PrimaryMission;

            // ----- 注册与初始化任务事件 -------------------------
            RegisterMissionEvent(mission);
            UpdateInfo(_primaryMissionItem, mission);
        }

        public void InitPreItem()
        {
            // 统计前置任务数量
            int preCount = MissionSystem.PrerequiredMissions.Count;

            // 遍历初始化前置任务
            for(int i = 0; i < preCount; i++)
            {
                var missionObj = Instantiate(_pf_preItem, transform);
                _preMissionItems.Add(new MissionItem(missionObj.transform));
                
                var mission = MissionSystem.PrerequiredMissions[i];

                // ----- 注册与初始化任务事件 -------------------------
                RegisterMissionEvent(mission);
                UpdateInfo(_preMissionItems[i], mission);
            }
        }

        private void UpdateInfo(MissionItem item, MissionDataModel mission)
        {
            if(mission.StepIndex.Value > mission.MissionConfig.MissionSteps.Length - 1)
            {
                return;
            }

            item.TipText.text = "";

            item.NameText.text = mission.MissionConfig.MissionName;
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

                item.TipText.text += $"{tip} ({curProgress}/{progress})\n";

                
            }

            StartCoroutine(RefreshLayOut());
        }      

        private void UpdateFinishedInfo(MissionItem item)
        {
            item.TipText.text = "已完成";
        }  
    }

    [Serializable]
    public class MissionItem
    {
        public RectTransform Item;
        public Text NameText;
        public Text TipText;
        

        public MissionItem(Transform parent)
        {
            Item = parent.GetComponent<RectTransform>();
            NameText = parent.Find("Title/Txt").GetComponent<Text>();
            TipText = parent.Find("Tip/Txt").GetComponent<Text>();
        }
    }
}