using System.Collections.Generic;
using QFramework.UtilityKit;
using QFramework.ViewController.UI;
using QFramework.Utility;
using UnityEngine;

namespace QFramework.Manager
{
    public enum UIGamePanelLayer
    {
        HUD = 0,
        Overlay = 1,
        Screen = 2,
        Modal = 3,
    }

    public enum UIGamePanelType
    {
        GameHUDPanel,
        InteractionPanel,
        InventoryPanel,
        PausePanel,
        GameOverPanel,
    }

    public class UIGameManager : MonoSingleton<UIGameManager>, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        private IInputUtility _input => this.GetUtility<IInputUtility>();

        public Canvas Canvas;

        // 面板注册
        private Dictionary<UIGamePanelType, AbstractBasePanel> _panelDict = new();
        private Dictionary<UIGamePanelType, UIGamePanelLayer> _panelLayers = new();
        private Dictionary<UIGamePanelType, bool> _panelHideLower = new();
        private Dictionary<UIGamePanelType, List<UIGamePanelType>> _hiddenTracker = new();

        protected override void Awake()
        {
            base.Awake();

            Canvas = GetComponent<Canvas>();

            InitPanelConfig();
            InitPanelDict();
        }

        private void Update()
        {
            if (_input.GetInventoryInput())
            {
                ToggleInventory();
            }
        }

        private void InitPanelConfig()
        {
            SetConfig(UIGamePanelType.GameHUDPanel,     UIGamePanelLayer.HUD,     false);
            SetConfig(UIGamePanelType.InteractionPanel, UIGamePanelLayer.Overlay, false);
            SetConfig(UIGamePanelType.InventoryPanel,   UIGamePanelLayer.Screen,  true);
            SetConfig(UIGamePanelType.PausePanel,       UIGamePanelLayer.Modal,   true);
            SetConfig(UIGamePanelType.GameOverPanel,    UIGamePanelLayer.Modal,   true);
        }

        private void SetConfig(UIGamePanelType type, UIGamePanelLayer layer, bool hideLower)
        {
            _panelLayers[type] = layer;
            _panelHideLower[type] = hideLower;
        }

        private void InitPanelDict()
        {
            void AddPanel(UIGamePanelType type)
            {
                var panelGo = transform.Find(type.ToString());
                if (panelGo != null && panelGo.TryGetComponent<AbstractBasePanel>(out var panel))
                {
                    _panelDict.Add(type, panel);
                }
            }

            AddPanel(UIGamePanelType.GameHUDPanel);
            AddPanel(UIGamePanelType.InteractionPanel);
            AddPanel(UIGamePanelType.InventoryPanel);
            AddPanel(UIGamePanelType.PausePanel);
            AddPanel(UIGamePanelType.GameOverPanel);

            // 按层设置渲染顺序（低层 → 低 sibling index → 先渲染）
            foreach (var kvp in _panelDict)
                kvp.Value.transform.SetSiblingIndex((int)_panelLayers[kvp.Key]);

            foreach (var panel in _panelDict.Values)
                panel.OnInit();
        }

        // ===== 公开 API =====

        private void ToggleInventory()
        {
            // Modal 层面板（Pause/GameOver）显示时不允许打开背包
            foreach (var kvp in _panelDict)
            {
                if (_panelLayers[kvp.Key] == UIGamePanelLayer.Modal && kvp.Value.gameObject.activeSelf)
                    return;
            }

            var panelType = UIGamePanelType.InventoryPanel;
            if (_panelDict.TryGetValue(panelType, out var panel) && panel.gameObject.activeSelf)
                HidePanel(panelType);
            else
                ShowPanel(panelType);
        }

        public void ShowPanel(UIGamePanelType panelType)
        {
            if (!_panelDict.TryGetValue(panelType, out var panel)) return;

            var layer = _panelLayers[panelType];

            if (_panelHideLower[panelType])
            {
                var hiddenList = new List<UIGamePanelType>();
                foreach (var kvp in _panelDict)
                {
                    if (_panelLayers[kvp.Key] < layer && kvp.Value.gameObject.activeSelf)
                    {
                        kvp.Value.gameObject.SetActive(false);
                        hiddenList.Add(kvp.Key);
                    }
                }
                _hiddenTracker[panelType] = hiddenList;
            }

            panel.Show();
            UpdateCursorState();
        }

        public void HidePanel(UIGamePanelType panelType)
        {
            if (!_panelDict.TryGetValue(panelType, out var panel)) return;

            var layer = _panelLayers[panelType];
            panel.Hide();

            bool hasBlocker = false;
            foreach (var kvp in _panelDict)
            {
                if (kvp.Key != panelType
                    && _panelLayers[kvp.Key] >= layer
                    && kvp.Value.gameObject.activeSelf)
                {
                    hasBlocker = true;
                    break;
                }
            }

            if (!hasBlocker && _hiddenTracker.TryGetValue(panelType, out var hiddenList))
            {
                foreach (var hiddenType in hiddenList)
                {
                    if (_panelDict.TryGetValue(hiddenType, out var hiddenPanel) && !hiddenPanel.gameObject.activeSelf)
                        hiddenPanel.Show();
                }
                _hiddenTracker.Remove(panelType);
            }

            UpdateCursorState();
        }

        private void UpdateCursorState()
        {
            foreach (var kvp in _panelDict)
            {
                if (kvp.Key != UIGamePanelType.GameHUDPanel && kvp.Value.gameObject.activeSelf)
                {
                    Cursor.visible = true;
                    return;
                }
            }
            Cursor.visible = false;
        }
    }
}
