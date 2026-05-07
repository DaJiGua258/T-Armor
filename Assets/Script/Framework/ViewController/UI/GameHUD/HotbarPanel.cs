using System;
using System.Collections.Generic;
using QFramework.Enum;
using QFramework.Event;
using QFramework.Command;
using QFramework.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    [Serializable]
    public struct HotbarInfoData
    {
        public Text NameTxt;  // 名称文本
        public Image NameImg;  // 名称背景图
        public Image[] CountImgs;  // 数量指示图
    }

    public class HotbarPanel : BaseUIComponent
    {
        [SerializeField] private Transform _contentRoot;  // 快捷栏内容根节点

        private List<HotbarInfoData> _hotbarInfos = new List<HotbarInfoData>();  // 所有槽位数据
        private Sprite _uiFrameSprite;  // 名称背景默认帧图
        private int _selectedIndex = -1;  // 当前选中槽位索引
        private bool _canUseItem = false;

        private HotbarExecutor _hotbarExecutor;

        private static readonly Color s_colorBlack = Color.black;
        private static readonly Color s_colorWhite = Color.white;

        void Start()
        {
            _hotbarExecutor = GetComponent<HotbarExecutor>();

            InitHotbar();
            RegisterEvents();
            UpdateHotbar();

            // 默认选中第一个非空槽位
            var firstItem = InvenotrySystem.GetHotbarItemByIndex(0);
            if (firstItem != null && firstItem.ItemType != ItemTypeEnum.None)
                SetSelectedIndex(0);
        }

        void Update()
        {
            // Tab 键循环切换选中物品（战斗/引导中禁用）
            if (this.GetUtility<IInputUtility>().GetHotbarCycleInput())
            {
                if (!_canUseItem || _hotbarExecutor.IsChanneling) return;

                int count = _hotbarInfos.Count;
                for (int i = 1; i <= count; i++)
                {
                    int idx = (_selectedIndex + i) % count;
                    var itemData = InvenotrySystem.GetHotbarItemByIndex(idx);
                    if (itemData != null && itemData.ItemType != ItemTypeEnum.None)
                    {
                        SetSelectedIndex(idx);
                        return;
                    }
                }
            }

            // 交互模式下点击使用选中物品（引导中禁用）
            if (_canUseItem && this.GetUtility<IInputUtility>().GetLeftMouseDownInput() && _selectedIndex >= 0 && !_hotbarExecutor.IsChanneling)
            {
                var itemData = InvenotrySystem.GetHotbarItemByIndex(_selectedIndex);
                if (itemData != null && itemData.ItemType != ItemTypeEnum.None && itemData.canUse)
                {
                    _hotbarExecutor.Execute(itemData.ItemType, _selectedIndex);
                }
            }
        }

        // 从 Content 根节点下获取所有槽位引用
        private void InitHotbar()
        {
            // 遍历所有子节点，收集槽位UI引用
            for (int i = 0; i < _contentRoot.childCount; i++)
            {
                var child = _contentRoot.GetChild(i);
                var nameImg = child.Find("Name")?.GetComponent<Image>();

                // 从首个槽位缓存默认帧图
                if (i == 0 && nameImg != null)
                    _uiFrameSprite = nameImg.sprite;

                var info = new HotbarInfoData
                {
                    NameTxt = child.Find("Name/Txt")?.GetComponent<Text>(),
                    NameImg = nameImg,
                    CountImgs = new Image[3]
                };

                var countGroup = child.Find("Count");
                for (int j = 0; j < 3; j++)
                    info.CountImgs[j] = countGroup?.GetChild(j)?.GetComponent<Image>();

                _hotbarInfos.Add(info);
            }
        }

        // 注册数据变化事件
        private void RegisterEvents()
        {
            // 遍历快捷栏槽位，监听数据变化
            for (int i = 0; i < InvenotrySystem.SupportItemCache.Count; i++)
            {
                var item = InvenotrySystem.GetHotbarItemByIndex(i);
                item.Count.RegisterOnValueChanged(UpdateHotbar);
                item.InstanceId.RegisterOnValueChanged(UpdateHotbar);
            }

            // 监听瞄准模式切换
            TypeEventSystem.Global.Register<PlayerEvent.SwitchAimingMode>(
                e =>
                {
                    _canUseItem = e.Mode == AimingModeEnum.Interaction;

                    if (e.Mode == AimingModeEnum.Combat)
                    {
                        ClearSelected();
                    }
                    else
                    {
                        // 自动选中第一个非空物品
                        for (int i = 0; i < _hotbarInfos.Count; i++)
                        {
                            var itemData = InvenotrySystem.GetHotbarItemByIndex(i);
                            if (itemData != null && itemData.ItemType != ItemTypeEnum.None)
                            {
                                SetSelectedIndex(i);
                                return;
                            }
                        }
                    }
                }
            ).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        /// <summary>
        /// 切换选中槽位，更新名称背景和字体颜色
        /// </summary>
        public void SetSelectedIndex(int index)
        {
            if (_selectedIndex == index) return;

            // 恢复上一个槽位的未选中状态
            if (_selectedIndex >= 0)
            {
                var prev = _hotbarInfos[_selectedIndex];
                prev.NameImg.sprite = _uiFrameSprite;
                prev.NameTxt.color = s_colorWhite;
            }

            _selectedIndex = index;

            var curr = _hotbarInfos[_selectedIndex];
            curr.NameImg.sprite = null;
            curr.NameTxt.color = s_colorBlack;

            // 选中物品时显示引导激光
            TypeEventSystem.Global.Send(new PlayerEvent.GuidanceLaserShow());
        }

        /// <summary>
        /// 清除选中状态，恢复为未选中样式
        /// </summary>
        public void ClearSelected()
        {
            if (_selectedIndex < 0) return;

            var prev = _hotbarInfos[_selectedIndex];
            prev.NameImg.sprite = _uiFrameSprite;
            prev.NameTxt.color = s_colorWhite;
            _selectedIndex = -1;

            // 取消选中时隐藏引导激光（引导中不隐藏，由 HotbarExecutor 控制）
            if (!_hotbarExecutor.IsChanneling)
                TypeEventSystem.Global.Send(new PlayerEvent.GuidanceLaserHide());
        }

        // 刷新所有热栏显示
        private void UpdateHotbar()
        {
            bool hasAnyItem = false;

            // 遍历所有槽位，同步数据显示
            for (int i = 0; i < _hotbarInfos.Count; i++)
            {
                var itemData = InvenotrySystem.GetHotbarItemByIndex(i);
                var info = _hotbarInfos[i];

                if (itemData == null || itemData.ItemType == ItemTypeEnum.None)
                {
                    info.NameTxt.text = "NONE";
                    info.NameTxt.color = s_colorWhite;
                    info.NameImg.sprite = _uiFrameSprite;
                    foreach (var img in info.CountImgs)
                        img.enabled = false;
                    continue;
                }

                hasAnyItem = true;
                info.NameTxt.text = itemData.name;

                int count = itemData.Count.Value;
                for (int j = 0; j < info.CountImgs.Length; j++)
                    info.CountImgs[j].enabled = (j < count);
            }

            // 当前选中槽位已变空时清除高亮
            if (_selectedIndex >= 0)
            {
                var selectedItem = InvenotrySystem.GetHotbarItemByIndex(_selectedIndex);
                if (selectedItem == null || selectedItem.ItemType == ItemTypeEnum.None)
                    ClearSelected();
            }

            // 所有槽位为空时清除选中
            if (!hasAnyItem)
                ClearSelected();
        }
    }
}
