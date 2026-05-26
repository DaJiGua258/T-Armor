using DG.Tweening;
using QFramework.Event;
using QFramework.Manager;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class LevelDetailPanel : AbstractBasePanel
    {
        [SerializeField] private Text _mapInfoText;         // 地图信息（地形/湿度/植被/威胁）
        [SerializeField] private Text _levelNameText;        // 任务名称
        [SerializeField] private Text _levelDescriptionText; // 任务介绍

        [Header("进入关卡")]
        [SerializeField] private Button _enterLevelBtn;

        [Header("滑入滑出")]
        [SerializeField] private float _slideDuration = 0.35f;
        [SerializeField] private float _hiddenY = -600f;

        private Tween _slideTween;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
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
            GameManager.Instance.EnterGameScene();
        }

        private void OnUpdateMapInfo(UpdateMapInfo e)
        {
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

            _mapInfoText.text = string.Format(
                "> 地形：{0}\n> 湿度：{1}\n> 植被：{2}\n> 威胁：暂无",
                LevelTypeModel.GetTerrainTypeName(envData.terrainType),
                LevelTypeModel.GetMoistureTypeName(envData.moistureType),
                LevelTypeModel.GetPlantLevelTypeName(envData.plantLevelType));

            _levelNameText.text = $"> {levelConfig.LevelName}";
            _levelDescriptionText.text = levelConfig.LevelDescription;
        }
    }
}
