using DG.Tweening;
using QFramework.Event;
using QFramework.Manager;
using QFramework.System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class LevelDetailPanel : AbstractBasePanel
    {
        [SerializeField] private Text _mapInfoText;         // 地图信息（地形/湿度/植被/威胁）
        [SerializeField] private Text _levelNameText;        // 关卡名称 / 统计标签
        [SerializeField] private Text _levelDescriptionText; // 关卡介绍（已废弃）

        [Header("进入关卡")]
        [SerializeField] private Button _enterLevelBtn;

        [Header("关卡记录")]
        [SerializeField] private Text _levelRecordText;

        [Header("滑入滑出")]
        [SerializeField] private float _slideDuration = 0.35f;
        [SerializeField] private float _hiddenY = -600f;

        private Tween _slideTween;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;

            var highlight = _enterLevelBtn.gameObject.AddComponent<UIHighlight>();
            highlight.Setup(
                _enterLevelBtn.GetComponent<Image>(),
                _enterLevelBtn.transform.Find("Txt").GetComponent<Text>()
            );
            AddHoverHandler(_enterLevelBtn, highlight);
        }

        public override void OnShow()
        {
            this.RegisterEvent<UpdateMapInfo>(OnUpdateMapInfo);

            _enterLevelBtn.onClick.AddListener(OnEnterLevelClick);

            // 从隐藏位置滑入
            _slideTween?.Kill();
            var pos = _rectTransform.anchoredPosition;
            pos.y = _hiddenY;
            _rectTransform.anchoredPosition = pos;
            _slideTween = _rectTransform.DOAnchorPosY(0, _slideDuration).SetEase(Ease.OutSine);
        }

        public override void OnHide()
        {
            this.UnRegisterEvent<UpdateMapInfo>(OnUpdateMapInfo);
            _enterLevelBtn.onClick.RemoveListener(OnEnterLevelClick);
        }

        public override void Hide()
        {
            OnHide();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);

            _slideTween?.Kill();
            _slideTween = _rectTransform.DOAnchorPosY(_hiddenY, _slideDuration).SetEase(Ease.InSine)
                .OnComplete(() => { gameObject.SetActive(false); });
        }

        private void OnEnterLevelClick()
        {
            // 读取玩家选中的支援物品，初始化到 PlayerSystem
            var playerConfigPanel = FindObjectOfType<PlayerConfigPanel>();
            if (playerConfigPanel != null)
            {
                var selectedItems = playerConfigPanel.GetSelectedSupportItems();
                var playerSystem = this.GetSystem<IPlayerSystem>();
                playerSystem.InitSupportItems(selectedItems);
            }

            GameManager.Instance.EnterGameScene();
        }

        private void AddHoverHandler(Button btn, UIHighlight highlight)
        {
            var trigger = btn.gameObject.AddComponent<EventTrigger>();

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => highlight.SetHighlight(true));
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => highlight.SetHighlight(false));
            trigger.triggers.Add(exit);
        }

        private void OnUpdateMapInfo(UpdateMapInfo e)
        {
            if (this == null) return;
            RefreshUI();
            StartCoroutine(RefreshLayoutRoutine());
        }

        private IEnumerator RefreshLayoutRoutine()
        {
            Canvas.ForceUpdateCanvases();
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        }

        private void RefreshUI()
        {
            var envData = LevelSystem.LoadedLevelData.EnvironmentData;
            var levelConfig = LevelSystem.LoadedLevelData.LevelMissionConfig;

            // 地图信息
            _mapInfoText.text = string.Format(
                "> 地形：{0}\n> 湿度：{1}\n> 植被：{2}\n> 威胁：暂无",
                LevelTypeModel.GetTerrainTypeName(envData.terrainType),
                LevelTypeModel.GetMoistureTypeName(envData.moistureType),
                LevelTypeModel.GetPlantLevelTypeName(envData.plantLevelType));

            // 检查当前关卡是否为已通关的历史关卡
            bool isCompleted = false;
            LevelDataModel completedData = null;

            foreach (var entry in LevelSystem.LevelDataCache)
            {
                if (entry?.EnvironmentData == null) continue;
                if (Vector3.Distance(entry.EnvironmentData.SurfaceNormal, envData.SurfaceNormal) < 0.001f)
                {
                    completedData = entry;
                    isCompleted = true;
                    break;
                }
            }

            if (isCompleted && completedData != null)
            {
                // 历史关卡：显示通关记录，隐藏部署按钮
                _levelRecordText.gameObject.SetActive(true);
                _enterLevelBtn.gameObject.SetActive(false);

                int minutes = Mathf.FloorToInt(completedData.CompletionTimeSeconds / 60f);
                int seconds = Mathf.FloorToInt(completedData.CompletionTimeSeconds % 60f);
                string timeStr = string.Format("{0}:{1:D2}", minutes, seconds);

                _levelNameText.text = string.Format(
                    "> 击杀数: \n> 造成伤害: \n> 受到伤害: \n> 开火数: \n> 时间: ");

                _levelRecordText.text = string.Format(
                    "{0}\n{1}\n{2}\n{3}\n{4}",
                    completedData.Kills,
                    completedData.DamageDealt,
                    completedData.DamageTaken,
                    completedData.ShotsFired,
                    timeStr);
            }
            else
            {
                // 新关卡：隐藏记录，显示部署按钮
                _levelRecordText.gameObject.SetActive(false);
                _enterLevelBtn.gameObject.SetActive(true);

                _levelNameText.text = $"> {levelConfig.LevelName}";
            }
        }
    }
}
