using System;
using System.Collections;
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
        [SerializeField] private Text _missionText;

        void Start()
        {
            InitMissionItem();
            StartCoroutine(RefreshLayOut());
        }

        private void InitMissionItem()
        {
            if(MissionSystem.Missions.Count <= 1) return;
            var mission = MissionSystem.Missions[1];
            if(mission.MissionType == MissionTypeEnum.None) return;

            RegisterMissionEvent(mission);
        }

        private void RegisterMissionEvent(MissionDataModel mission)
        {
            mission.StepIndex.Register(_ => UpdateUI(mission))
                .UnRegisterWhenGameObjectDestroyed(gameObject);
            mission.MissionState.Register(_ => UpdateUI(mission))
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            foreach(var step in mission.StepList)
            {
                step.Register(_ => UpdateUI(mission))
                    .UnRegisterWhenGameObjectDestroyed(gameObject);
            }

            UpdateUI(mission);
        }

        private void UpdateUI(MissionDataModel mission)
        {
            string Gray(string s) => $"<color=#808080>{s}</color>";

            string[] lines = { "找到信标", "激活信标", "撤离" };
            int step = mission.StepIndex.Value;
            var steps = mission.MissionConfig.MissionSteps;

            string text = "";
            for(int i = 0; i < lines.Length; i++)
            {
                string line;
                bool active = i == step;
                bool done = i < step;

                if(i == 0)
                {
                    line = done ? $"> {lines[i]} （已完成）" : $"> {lines[i]}";
                }
                else if(i == 1)
                {
                    if(step == 1)
                    {
                        int cur = mission.StepList[1].Value;
                        if(cur > 0)
                        {
                            int max = steps[1].Progress;
                            int remaining = Mathf.Max(0, max - cur);
                            line = $"> {lines[i]}（{remaining / 60}:{remaining % 60:D2}）";
                        }
                        else
                        {
                            line = $"> {lines[i]}";
                        }
                    }
                    else if(done)
                        line = $"> {lines[i]} （已完成）";
                    else
                        line = $"> {lines[i]}";
                }
                else if(i == 2)
                {
                    line = $"> {lines[i]}";
                }
                else continue;

                text += (active ? line : Gray(line)) + "\n";
            }

            _missionText.text = text.TrimEnd('\n');
            if(gameObject.activeInHierarchy)
                StartCoroutine(RefreshLayOut());
        }

        IEnumerator RefreshLayOut()
        {
            Canvas.ForceUpdateCanvases();
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        }
    }

    // 注释：简化后不再使用 MissionItem，直接用 Text 组件
    // [Serializable]
    // public class MissionItem
    // {
    //     public RectTransform Item;
    //     public Image TileImg;
    //     public Text NameText;
    //     public Text TipText;

    //     public MissionItem(Transform parent)
    //     {
    //         Item = parent.GetComponent<RectTransform>();
    //         TileImg = parent.Find("Title").GetComponent<Image>();
    //         NameText = parent.Find("Title/Txt").GetComponent<Text>();
    //         TipText = parent.Find("Tip/Txt").GetComponent<Text>();
    //     }
    // }
}
