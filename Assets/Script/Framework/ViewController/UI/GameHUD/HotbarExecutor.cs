using QFramework.Command;
using QFramework.Enum;
using QFramework.Event;
using QFramework.Utility;
using UnityEngine;

namespace QFramework.ViewController.UI
{
    public class HotbarExecutor : BaseUIComponent
    {
        [Header("信标预制体（含 Emitter + GuidanceState）")]
        public GameObject AirStrikesPrefab;
        public GameObject AirSupportPrefab;
        public GameObject ArtilleryPrefab;
        public GameObject MissilePrefab;

        public bool IsChanneling => _isChanneling;

        private bool _isChanneling;
        private int _channelIndex;

        void Update()
        {
            if (!_isChanneling) return;

            // 提前松开鼠标则中断引导
            if (this.GetUtility<IInputUtility>().GetLeftMouseUpInput())
                Interrupt();
        }

        public void Execute(ItemTypeEnum itemType, int index)
        {
            if (_isChanneling) return;

            // 按下立即消耗一次使用次数
            this.SendCommand(PlayerCommand.UseHotbarItem.Instance.Init(index));

            _isChanneling = true;
            _channelIndex = index;

            TypeEventSystem.Global.Send(new PlayerEvent.GuidanceLaserSetChanneling());

            // 生成对应的信标预制体（含 Emitter + GuidanceState，自动开火并控制激光）
            GameObject prefab = GetMarkerPrefab(itemType);
            if (prefab != null)
            {
                Vector3 spawnPos = this.GetUtility<IInputUtility>().GetMousePos();
                spawnPos.z = 0f;
                Instantiate(prefab, spawnPos, Quaternion.identity);
            }
        }

        private GameObject GetMarkerPrefab(ItemTypeEnum itemType)
        {
            return itemType switch
            {
                ItemTypeEnum.Marker_AirStrikes => AirStrikesPrefab,
                ItemTypeEnum.Marker_AirSupport => AirSupportPrefab,
                ItemTypeEnum.Marker_Artillery => ArtilleryPrefab,
                ItemTypeEnum.Marker_Missile => MissilePrefab,
                _ => null
            };
        }

        public void Interrupt()
        {
            if (!_isChanneling) return;
            _isChanneling = false;
            TypeEventSystem.Global.Send(new PlayerEvent.GuidanceLaserHide());
        }
    }
}
