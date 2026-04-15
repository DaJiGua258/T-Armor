using QFramework.Event;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class MapInfoDetailContainer : AbstractBasePanel
    {
        [SerializeField] private MapInfoDetailItem _terrainItem;
        [SerializeField] private MapInfoDetailItem _moistureItem;
        [SerializeField] private MapInfoDetailItem _plantItem;
        [SerializeField] private MapInfoDetailItem _danger;

        /// <summary>
        /// 初始化地图信息详情容器
        /// </summary>
        public void InitMapInfoDetail()
        {
            _terrainItem = new MapInfoDetailItem("TerrainType", transform);
            _moistureItem = new MapInfoDetailItem("MoistureType", transform);
            _plantItem = new MapInfoDetailItem("PlantType", transform);
            _danger = new MapInfoDetailItem("DangerType", transform);

            // LevelSystem.CurrentSelectLevelData.seed.RegisterOnValueChanged(UpdateMapDetailInfo);

            this.RegisterEvent<UpdateMapInfo>(e => UpdateMapDetailInfo());
        }

        /// <summary>
        /// 更新地图信息详情容器
        /// </summary>
        public void UpdateMapDetailInfo()
        {
            var currentLevel = LevelSystem.CurrentSelectLevelData;
            Debug.Log("seed: " + currentLevel.seed.Value);
            _terrainItem.Info.text = LevelTypeModel.GetTerrainTypeName(currentLevel.environmentData.terrainType);
            _moistureItem.Info.text = LevelTypeModel.GetMoistureTypeName(currentLevel.environmentData.moistureType);
            _plantItem.Info.text = LevelTypeModel.GetPlantLevelTypeName(currentLevel.environmentData.plantLevelType);
            // _danger.Info.text = currentLevel.DangerType.ToString();
        }

    }

    /// <summary>
    /// 地图信息详情项容器
    /// </summary>
    public class MapInfoDetailItem
    {
        public Sprite Icon;
        public Text IconName;
        public Text Info;

        public MapInfoDetailItem(string itemName, Transform parent)
        {
            Transform itemTransform = parent.Find(itemName);
            Icon = itemTransform.Find("Icon_Img").GetComponent<Image>().sprite;
            IconName = itemTransform.GetChild(0).Find("IconName_Txt").GetComponent<Text>();
            Info = itemTransform.Find("Info").GetComponent<Text>();
        }
    }
}
