using System.Collections;
using System.Collections.Generic;
using QFramework.UtilityKit;
using QFramework.ViewController.UI;
using QFramework.Utility;
using UnityEngine;

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
        private Dictionary<UIPanelType, BasePanel> panelDict = new();
        
        // Canvas 下的层级节点
        [SerializeField] private Transform normalLayer;   // 普通界面层
        [SerializeField] private Transform popupLayer;    // 弹窗层
        [SerializeField] private Transform topLayer;      // 顶层（Loading等）

        protected override void Awake()
        {
            base.Awake();

            // 初始化
            var gameHUD = Instantiate(_resourceLoad.Load<GameObject>("Prefab/UI/" + UIPanelType.GameHUD.ToString()), transform);
            var interaction = Instantiate(_resourceLoad.Load<GameObject>("Prefab/UI/" + UIPanelType.Interaction.ToString()), transform);
            var inventory = Instantiate(_resourceLoad.Load<GameObject>("Prefab/UI/" + UIPanelType.Inventory.ToString()), transform);
            var pause = Instantiate(_resourceLoad.Load<GameObject>("Prefab/UI/" + UIPanelType.Pause.ToString()), transform);

            panelDict.Add(UIPanelType.GameHUD, gameHUD.GetComponent<GameHUDPanel>());
            panelDict.Add(UIPanelType.Interaction, interaction.GetComponent<InteractionPanel>());
            panelDict.Add(UIPanelType.Inventory, inventory.GetComponent<InventoryPanel>());
            panelDict.Add(UIPanelType.Pause, pause.GetComponent<PausePanel>());
        }

        /// <summary>
        /// 
        /// </summary>
        public void ShowPanel(UIPanelType panelType)
        {
            if (panelDict.TryGetValue(panelType, out BasePanel panel))
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
        
    }
}
