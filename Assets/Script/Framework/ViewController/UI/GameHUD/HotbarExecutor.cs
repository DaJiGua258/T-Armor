using QFramework.Command;
using QFramework.Enum;
using QFramework.Event;
using QFramework.Utility;
using QFramework.ViewController.Misc;
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
        private GuidanceState _currentGuidance;

        void Update()
        {
            if (!_isChanneling) return;

            if (this.GetUtility<IInputUtility>().GetLeftMouseUpInput())
                Interrupt();
        }

        public void Execute(ItemTypeEnum itemType, int index)
        {
            if (_isChanneling) return;

            // 消耗使用次数
            this.SendCommand(PlayerCommand.UseHotbarItem.Instance.Init(index));

            // 替换为新的引导：取消订阅旧的、锁定旧的位置
            ReleaseCurrentGuidance();

            _isChanneling = true;
            _channelIndex = index;

            // 生成信标预制体
            GameObject prefab = GetMarkerPrefab(itemType);
            if (prefab == null) return;

            Vector3 spawnPos = this.GetUtility<IInputUtility>().GetMousePos();
            spawnPos.z = 0f;
            var go = Instantiate(prefab, spawnPos, Quaternion.identity);
            _currentGuidance = go.GetComponent<GuidanceState>();
            if (_currentGuidance == null) return;

            // 订阅回调后再开火
            _currentGuidance.OnChannelingEnded += OnCurrentGuidanceEnded;

            TypeEventSystem.Global.Send(new PlayerEvent.GuidanceLaserSetChanneling());
            _currentGuidance.Fire();
        }

        private void ReleaseCurrentGuidance()
        {
            if (_currentGuidance == null) return;

            _currentGuidance.OnChannelingEnded -= OnCurrentGuidanceEnded;
            _currentGuidance.LockMouse();
            _currentGuidance = null;
        }

        private void OnCurrentGuidanceEnded()
        {
            _isChanneling = false;
            _currentGuidance = null;
            TypeEventSystem.Global.Send(new PlayerEvent.GuidanceLaserHide());
        }

        private void Interrupt()
        {
            if (!_isChanneling) return;

            if (_currentGuidance != null)
                _currentGuidance.LockMouse();
            _isChanneling = false;
            _currentGuidance = null;
            TypeEventSystem.Global.Send(new PlayerEvent.GuidanceLaserHide());
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
    }
}
