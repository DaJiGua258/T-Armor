using System.Collections.Generic;
using QFramework.Event;
using QFramework.UtilityKit;
using QFramework.ViewController.UI;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.Manager
{
    public enum UIMainMenuPanelType
    {
        // 主菜单流程
        MainMenuPanel,           // 主菜单（新游戏/继续/加载/设置/退出）
        LevelSelectPanel,        // 选关界面
        LevelDetailPanel,        // 关卡详情/确认
        EquipmentConfigPanel,    // 玩家装备配置
        
        // 通用
        SettingsPanel,           // 设置（从主菜单或其他地方进入）
        LoadGamePanel,           // 加载存档列表
    }

    public class MainUIManager : MonoSingleton<MainUIManager>, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        // 挂载节点
        [SerializeField] private Transform _canvasWorldSpace;
        [SerializeField] private Transform _canvasScreenSpace;

        // 屏幕空间面板字典
        private Dictionary<UIMainMenuPanelType, AbstractBasePanel> _panelDict = new();

        // 世界空间面板
        public PlanetNodeList PlanetNodeList;

        // 世界空间游戏物体
        public PlanetGenerator PlanetGenerator;

        // 主菜单全局共享资源
        public List<GameObject> NodeS;
        public Camera Camera;
        public Vector3 StartCameraPosition;
        public Quaternion StartCameraRotation;


        protected override void Awake()
        {
            base.Awake();

            // 获取挂载节点
            _canvasWorldSpace = transform.Find("CanvasWorldSpace");
            _canvasScreenSpace = transform.Find("CanvasScreenSpace");

            // 获取世界空间的UI游戏物体
            InitGameWorld();

            // 获取UI面板
            InitWorldPanelDict();
            InitScreenPanelDict();

            // 初始化主菜单世界UI数据
            InitMainMenuWorldDataInOrder();

            // 初始
            foreach (var node in NodeS)
            {
                node.GetComponent<Button>().onClick.AddListener(() => EnterLevelConfirm());
            }


            PlanetNodeList.gameObject.SetActive(false);
            
            Camera = Camera.main;
            ShowPanelOnly(UIMainMenuPanelType.MainMenuPanel);
        }

        void Start()
        {
            StartCameraPosition = Camera.transform.position;
            StartCameraRotation = Camera.transform.rotation;
            EnterMainMenu();
        }

        
        #region ----- 基础面板事件 ------------------------------
        /// <summary>
        /// 初始化屏幕面板
        /// </summary>
        private void InitScreenPanelDict()
        {
            void AddPanel(UIMainMenuPanelType type)
            {
                if (_canvasScreenSpace.Find(type.ToString()).TryGetComponent<AbstractBasePanel>(out AbstractBasePanel panel)) _panelDict.Add(type, panel);
            }

            AddPanel(UIMainMenuPanelType.MainMenuPanel);
            AddPanel(UIMainMenuPanelType.LevelSelectPanel);
            AddPanel(UIMainMenuPanelType.LevelDetailPanel);
            AddPanel(UIMainMenuPanelType.EquipmentConfigPanel);
            AddPanel(UIMainMenuPanelType.SettingsPanel);
            AddPanel(UIMainMenuPanelType.LoadGamePanel);
        }

        /// <summary>
        /// 初始化世界空间下的面板
        /// </summary>
        private void InitWorldPanelDict()
        {
            PlanetNodeList = _canvasWorldSpace.Find("PlanetNodeList").GetComponent<PlanetNodeList>();
        }

        /// <summary>
        /// 初始化游戏物体
        /// </summary>
        private void InitGameWorld()
        {
            PlanetGenerator = GameObject.Find("Planet").GetComponent<PlanetGenerator>();
            
        }

        /// <summary>
        /// 主菜单统一顺序初始化：
        /// 1) 生成星球纹理
        /// 2) 同步读回GPU噪声到CPU缓存
        /// 3) 生成符合地形规则的节点
        /// </summary>
        private void InitMainMenuWorldDataInOrder()
        {
            if (PlanetGenerator == null || PlanetNodeList == null)
            {
                Debug.LogWarning("MainUIManager: PlanetGenerator 或 PlanetNodeList 未准备好。");
                return;
            }

            PlanetGenerator.Generate();
            if (!PlanetGenerator.BuildNoiseCacheSync())
            {
                Debug.LogWarning("MainUIManager: 星球噪声读回失败，节点可能为空。");
            }

            NodeS = PlanetNodeList.InitNodes(PlanetGenerator);
        }

        /// <summary>
        /// 只显示当前类型的Panel，其余的全部关闭
        /// </summary>
        private void ShowPanelOnly(UIMainMenuPanelType type)
        {
            if (!_panelDict.TryGetValue(type, out var panel))
            {
                panel = CreatePanel(type);
                _panelDict[type] = panel;
            }
            
            panel.Show();
        }

        /// <summary>
        /// 显示当前类型的Panel，其余的状态不变
        /// </summary>
        private void ShowPanel(UIMainMenuPanelType type)
        {
            if (_panelDict.TryGetValue(type, out var panel))
                panel.Show();
        }

        /// <summary>
        /// 只关闭当前类型的Panel，其余的状态不变
        /// </summary>
        private void HidePanel(UIMainMenuPanelType type)
        {
            if (_panelDict.TryGetValue(type, out var panel))
                panel.Hide();
        }

        /// <summary>
        /// 关闭所有Panel
        /// </summary>
        private void HideAll()
        {
            foreach (var panel in _panelDict.Values)
                panel.Hide();
        }

        private AbstractBasePanel CreatePanel(UIMainMenuPanelType type)
        {
            string path = $"UIPanels/{type}";
            var prefab = Resources.Load<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogError($"找不到面板预制体: {path}");
                return null;
            }

            var go = Instantiate(prefab, _canvasScreenSpace);
            return go.GetComponent<AbstractBasePanel>();
        }

        #endregion

        #region ----- 流程入口 ------------------------------

        /// <summary>
        /// 进入主菜单
        /// </summary>
        public void EnterMainMenu()
        {
            HideAll();
            ShowPanelOnly(UIMainMenuPanelType.MainMenuPanel);
        }

        /// <summary>
        /// 进入选关面板
        /// </summary>
        public void EnterLevelSelect()
        {
            HideAll();
            ShowPanelOnly(UIMainMenuPanelType.LevelSelectPanel);
        }

        /// <summary>
        /// 点击星球节点后，进入关卡详情面板
        /// </summary>
        public void EnterLevelConfirm()
        {
            ShowPanelOnly(UIMainMenuPanelType.LevelDetailPanel);
            ShowPanelOnly(UIMainMenuPanelType.EquipmentConfigPanel);
            Debug.Log("EnterLevelConfirm");
        }

        #endregion

        #region ----- 选关面板事件 ------------------------------

        private void ShowLevelSelectPanel()
        {
            
        }

        #endregion

        #region ----- 其他 ------------------------------

        public void ResetCamera()
        {
            Camera.transform.SetPositionAndRotation(StartCameraPosition, StartCameraRotation);
        }

        #endregion


    }
}