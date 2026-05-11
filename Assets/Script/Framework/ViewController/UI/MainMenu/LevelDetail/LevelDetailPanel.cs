using QFramework.Event;
using UnityEngine.UI;
using UnityEngine;

namespace QFramework.ViewController.UI
{
    public class LevelDetailPanel : AbstractBasePanel
    {
        [SerializeField] private Text _mapInfoText;         // 地图信息（地形/湿度/植被/威胁）
        [SerializeField] private Text _levelNameText;        // 任务名称
        [SerializeField] private Text _levelDescriptionText; // 任务介绍

        public override void OnShow()
        {
            this.RegisterEvent<UpdateMapInfo>(OnUpdateMapInfo);
        }

        public override void OnHide()
        {
            this.UnRegisterEvent<UpdateMapInfo>(OnUpdateMapInfo);
        }

        private void OnUpdateMapInfo(UpdateMapInfo e)
        {
            RefreshUI();
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
