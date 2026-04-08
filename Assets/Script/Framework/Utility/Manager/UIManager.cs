using System.Collections;
using System.Collections.Generic;
using QFramework.UtilityKit;
using QFramework.ViewController.UI;
using QFramework.Utility;
using UnityEngine;
using System;

namespace QFramework.Utility.Manager
{
    public enum UIPanelType
    {
        GameHUD,
        Interaction,
        Inventory,
        Pause,
    }
    

    public class UIManager : MonoSingleton<UIManager>, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        private IResourceLoad _resourceLoad => this.GetUtility<IResourceLoad>();

        // 存储已创建的面板实例
        private Dictionary<UIPanelType, AbstractBasePanel> panelDict = new();
        
        // Canvas 下的层级节点
        [SerializeField] private Transform normalLayer;   // 普通界面层
        [SerializeField] private Transform popupLayer;    // 弹窗层
        [SerializeField] private Transform topLayer;      // 顶层（Loading等）

        protected override void Awake()
        {
            base.Awake();

            // 初始化
            // var gameHUD = Instantiate(_resourceLoad.Load<GameObject>("Prefab/UI/Panel/" + UIPanelType.GameHUD.ToString()), transform);
            // var interaction = Instantiate(_resourceLoad.Load<GameObject>("Prefab/UI/Panel/" + UIPanelType.Interaction.ToString()), transform);
            // var inventory = Instantiate(_resourceLoad.Load<GameObject>("Prefab/UI/Panel/" + UIPanelType.Inventory.ToString()), transform);
            // var pause = Instantiate(_resourceLoad.Load<GameObject>("Prefab/UI/Panel/" + UIPanelType.Pause.ToString()), transform);

            // panelDict.Add(UIPanelType.GameHUD, gameHUD.GetComponent<GameHUDPanel>());
            // panelDict.Add(UIPanelType.Interaction, interaction.GetComponent<InteractionPanel>());
            // panelDict.Add(UIPanelType.Inventory, inventory.GetComponent<InventoryPanel>());
            // panelDict.Add(UIPanelType.Pause, pause.GetComponent<PausePanel>());

            // TryInitPanel(UIPanelType.GameHUD);
            // TryInitPanel(UIPanelType.Interaction);
            // TryInitPanel(UIPanelType.Inventory);
            // TryInitPanel(UIPanelType.Pause);
        }

        /// <summary>
        /// 
        /// </summary>
        public void ShowPanel(UIPanelType panelType)
        {
            if (panelDict.TryGetValue(panelType, out AbstractBasePanel panel))
            {
                panel.Show();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void HidePanel(UIPanelType type)
        {
            if (panelDict.TryGetValue(type, out var panel))
                panel.Hide();
        }
        
        private void TryInitPanel(UIPanelType panelType)
        {
            if (panelDict.TryGetValue(panelType, out AbstractBasePanel panel))
            {
                return;
            }
            else
            {
                var panelObject = Instantiate(_resourceLoad.Load<GameObject>("Prefab/UI/Panel/" + panelType.ToString()), transform);
                panel = panelObject.GetComponent<AbstractBasePanel>();
                panelDict.Add(panelType, panel);
            }
        }
    }
}
