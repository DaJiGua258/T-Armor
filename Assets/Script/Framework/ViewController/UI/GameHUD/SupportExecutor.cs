using QFramework.Command;
using QFramework.Enum;
using QFramework.Event;
using QFramework.Utility;
using QFramework.ViewController.Misc;
using UnityEngine;

namespace QFramework.ViewController.UI
{
    public class SupportExecutor : BaseUIComponent
    {
        private const string PrefabBasePath = "Prefab/Support/";

        public bool IsChanneling => _isChanneling;

        private bool _isChanneling;
        private int _channelIndex;
        private GuidanceState _currentGuidance;

        void Update()
        {
            // 冷却 ticking
            for (int i = 0; i < PlayerSystem.SupportItems.Count; i++)
            {
                var item = PlayerSystem.GetSupportItemByIndex(i);
                if (item != null && item.CooldownRemaining.Value > 0f)
                    item.CooldownRemaining.Value -= Time.deltaTime;
            }

            if (!_isChanneling) return;

            if (this.GetUtility<IInputUtility>().GetLeftMouseUpInput())
                Interrupt();
        }

        public void Execute(SupportTypeEnum itemType, int index)
        {
            if (_isChanneling) return;

            // 消耗使用次数（设置冷却）
            this.SendCommand(PlayerCommand.UseSupportItem.Instance.Init(index));

            // 替换为新的引导：取消订阅旧的、锁定旧的位置
            ReleaseCurrentGuidance();

            _isChanneling = true;
            _channelIndex = index;

            // 从 Resources 路径加载信标预制体
            string path = PrefabBasePath + itemType.ToString();
            GameObject prefab = this.GetUtility<IResourceLoad>().Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError("Support prefab not found: " + path);
                return;
            }

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
    }
}
