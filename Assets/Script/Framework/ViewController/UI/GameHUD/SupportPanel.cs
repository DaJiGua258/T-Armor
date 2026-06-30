using System;
using System.Collections.Generic;
using QFramework.Enum;
using QFramework.Event;
using QFramework.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    [Serializable]
    public struct SupportInfoData
    {
        public Image IconImg;
        public Text NameTxt;
        public Image CooldownBar;
    }

    public class SupportPanel : BaseUIComponent
    {
        [SerializeField] private Transform _contentRoot;

        private List<SupportInfoData> _supportInfos = new List<SupportInfoData>();
        private List<UIHighlight> _supportHighlights = new List<UIHighlight>();
        private int _selectedIndex = -1;
        private bool _canUseItem = false;
        private bool _isCombatMode = true;

        private SupportExecutor _supportExecutor;

        void Start()
        {
            _supportExecutor = GetComponent<SupportExecutor>();

            InitSupportBar();
            RegisterEvents();

            // 刷新布局
            UpdateSupportBar();
        }

        void Update()
        {
            // Tab 键循环切换选中物品（战斗/引导中禁用）
            if (this.GetUtility<IInputUtility>().GetSupportCycleInput())
            {
                if (!_canUseItem || _supportExecutor.IsChanneling) return;

                int count = _supportInfos.Count;
                for (int i = 1; i <= count; i++)
                {
                    int idx = (_selectedIndex + i) % count;
                    var itemData = PlayerSystem.GetSupportItemByIndex(idx);
                    if (itemData != null && itemData.SupportType != SupportTypeEnum.None)
                    {
                        // 冷却中的物品跳过
                        if (itemData.CooldownRemaining.Value > 0f)
                            continue;
                        SetSelectedIndex(idx);
                        return;
                    }
                }
            }

            // 交互模式下点击使用选中物品（引导中禁用）
            if (_canUseItem && this.GetUtility<IInputUtility>().GetLeftMouseDownInput() && _selectedIndex >= 0 && !_supportExecutor.IsChanneling)
            {
                var itemData = PlayerSystem.GetSupportItemByIndex(_selectedIndex);
                if (itemData != null && itemData.SupportType != SupportTypeEnum.None && itemData.canUse)
                {
                    // 冷却中无法使用
                    if (itemData.CooldownRemaining.Value > 0f)
                        return;

                    _supportExecutor.Execute(itemData.SupportType, _selectedIndex);
                }
            }
        }

        private void InitSupportBar()
        {
            for (int i = 0; i < _contentRoot.childCount; i++)
            {
                var child = _contentRoot.GetChild(i);

                var iconImg = child.Find("Slot/Icon_Img")?.GetComponent<Image>();

                var nameImg = child.Find("Name")?.GetComponent<Image>();
                var nameTxt = child.Find("Name/Txt")?.GetComponent<Text>();

                // 高亮组件挂在 Name 节点
                var highlight = child.Find("Name")?.gameObject.AddComponent<UIHighlight>();
                if (highlight != null && nameImg != null && nameTxt != null)
                {
                    highlight.Setup(nameImg, nameTxt);
                    _supportHighlights.Add(highlight);
                }

                // 冷却条 Fill 子节点
                var cooldownGroup = child.Find("CoolDown");
                Image cooldownBar = null;
                if (cooldownGroup != null && cooldownGroup.childCount > 0)
                    cooldownBar = cooldownGroup.GetChild(0).GetComponent<Image>();

                var info = new SupportInfoData
                {
                    IconImg = iconImg,
                    NameTxt = nameTxt,
                    CooldownBar = cooldownBar,
                };

                _supportInfos.Add(info);
            }
        }

        private void RegisterEvents()
        {
            for (int i = 0; i < PlayerSystem.SupportItems.Count; i++)
            {
                var item = PlayerSystem.GetSupportItemByIndex(i);
                item.CooldownRemaining.RegisterOnValueChanged(UpdateSupportBar)
                    .UnRegisterWhenGameObjectDestroyed(gameObject);
            }

            TypeEventSystem.Global.Register<PlayerEvent.SwitchAimingMode>(
                e =>
                {
                    _canUseItem = e.Mode == AimingModeEnum.Interaction;

                    _isCombatMode = e.Mode == AimingModeEnum.Combat;

                    if (_isCombatMode)
                    {
                        ClearAllHighlights();
                    }
                    else
                    {
                        // 自动选中第一个非空且非冷却物品
                        for (int i = 0; i < _supportInfos.Count; i++)
                        {
                            var itemData = PlayerSystem.GetSupportItemByIndex(i);
                            if (itemData != null && itemData.SupportType != SupportTypeEnum.None && itemData.CooldownRemaining.Value <= 0f)
                            {
                                SetSelectedIndex(i);
                                return;
                            }
                        }
                    }
                }
            ).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        public void SetSelectedIndex(int index)
        {
            if (_selectedIndex == index) return;
            if (_isCombatMode) return;

            if (_selectedIndex >= 0)
                _supportHighlights[_selectedIndex].SetHighlight(false);

            _selectedIndex = index;

            _supportHighlights[_selectedIndex].SetHighlight(true);

            TypeEventSystem.Global.Send(new PlayerEvent.GuidanceLaserShow());
        }

        public void ClearSelected()
        {
            if (_selectedIndex < 0) return;

            _supportHighlights[_selectedIndex].SetHighlight(false);
            _selectedIndex = -1;

            if (!_supportExecutor.IsChanneling)
                TypeEventSystem.Global.Send(new PlayerEvent.GuidanceLaserHide());
        }

        private void ClearAllHighlights()
        {
            _selectedIndex = -1;
            for (int i = 0; i < _supportHighlights.Count; i++)
                _supportHighlights[i].SetHighlight(false);
            if (!_supportExecutor.IsChanneling)
                TypeEventSystem.Global.Send(new PlayerEvent.GuidanceLaserHide());
        }

        private void UpdateSupportBar()
        {
            bool hasAnyItem = false;

            for (int i = 0; i < _supportInfos.Count; i++)
            {
                var itemData = PlayerSystem.GetSupportItemByIndex(i);
                var info = _supportInfos[i];

                if (itemData == null || itemData.SupportType == SupportTypeEnum.None)
                {
                    info.NameTxt.text = "NONE";
                    if (info.IconImg != null)
                    {
                        info.IconImg.sprite = null;
                        info.IconImg.enabled = false;
                    }
                    if (info.CooldownBar != null)
                        info.CooldownBar.fillAmount = 0f;
                    _supportHighlights[i].SetHighlight(false);
                    continue;
                }

                hasAnyItem = true;
                info.NameTxt.text = itemData.name;

                // 图标
                if (info.IconImg != null)
                {
                    if (!string.IsNullOrEmpty(itemData.iconPath))
                    {
                        var sprite = Resources.Load<Sprite>(itemData.iconPath);
                        info.IconImg.sprite = sprite;
                        info.IconImg.enabled = true;
                    }
                    else
                    {
                        info.IconImg.enabled = false;
                    }
                }

                // 冷却条
                if (info.CooldownBar != null)
                {
                    float remaining = itemData.CooldownRemaining.Value;
                    float total = itemData.cooldownTime;
                    info.CooldownBar.fillAmount = total > 0f ? 1f - remaining / total : 0f;
                }
            }

            if (_selectedIndex >= 0)
            {
                var selectedItem = PlayerSystem.GetSupportItemByIndex(_selectedIndex);
                if (selectedItem == null || selectedItem.SupportType == SupportTypeEnum.None)
                    ClearSelected();
            }

            if (!hasAnyItem)
                ClearSelected();
        }
    }
}
