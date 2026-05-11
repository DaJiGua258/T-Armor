using System.Collections.Generic;
using DG.Tweening;
using QFramework.System;
using QFramework.Utility;
using QFramework.UtilityKit;
using QFramework.ViewController.UI;
using UnityEngine;

namespace QFramework.Manager
{
    public enum UIMainPanelType
    {
        // 主菜单流程
        MainMenuPanel,           // 主菜单（新游戏/继续/加载/设置/退出）
        LevelSelectPanel,        // 选关界面
        LevelDetailPanel,        // 关卡详情/确认
        PlayerConfigPanel,       // 玩家信息 + 装备配置

        // 通用（注释：暂时不用，保留枚举值）
        // SettingsPanel,        // 设置（从主菜单或其他地方进入）
        // LoadGamePanel,        // 加载存档列表
    }

    /// <summary>
    /// 将多个 Panel 打包为一个导航单位
    /// </summary>
    public class UIMainPanelGroup
    {
        public List<UIMainPanelType> Panels { get; } = new();

        public UIMainPanelGroup(params UIMainPanelType[] panels)
        {
            Panels.AddRange(panels);
        }
    }

    public class MainUIManager : MonoSingleton<MainUIManager>, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        private ILevelSystem _levelSystem => this.GetSystem<ILevelSystem>();

        // ----- 挂载节点 ------------------------------
        [SerializeField] private Transform _canvasWorldSpace;
        [SerializeField] private Transform _canvasScreenSpace;

        // ----- 屏幕空间面板字典 ------------------------------
        private Dictionary<UIMainPanelType, AbstractBasePanel> _panelDict = new();
        private Stack<UIMainPanelGroup> _panelStack = new();
        private bool _isTransitioning;
        private Sequence _currentSequence;
        public UIMainPanelType CurrentPanel => _panelStack.Count > 0 ? _panelStack.Peek().Panels[0] : UIMainPanelType.MainMenuPanel;

        // ----- 世界空间面板 ------------------------------
        public PlanetNodeList PlanetNodeList;

        // ----- 世界空间游戏物体 ------------------------------
        public PlanetGenerator PlanetGenerator;
        public PlanetOrbitCamera OrbitOrbitCamera;

        // ----- 主菜单全局共享资源 ------------------------------
        private List<GameObject> _nodeS = new();
        public IReadOnlyList<GameObject> NodeS => _nodeS;
        public Camera MainCamera;
        public Vector3 StartCameraPosition;
        public Quaternion StartCameraRotation;

        [Header("镜头过渡")]
        [SerializeField] private float _cameraTransitionDuration = 1f;
        [SerializeField] private float _panelFadeDuration = 0.3f;
        [SerializeField] private Ease _cameraEase = Ease.InOutSine;
        [SerializeField] private Ease _fadeEase = Ease.OutQuad;
        [SerializeField] private Vector3 _levelSelectDirection = new Vector3(0f, 0.342f, -0.94f);


        protected override void Awake()
        {
            base.Awake();

            Canvas.ForceUpdateCanvases();

            _canvasWorldSpace = transform.Find("CanvasWorldSpace");
            _canvasScreenSpace = transform.Find("CanvasScreenSpace");

            InitGameWorld();
            InitWorldPanelDict();
            InitScreenPanelDict();
            InitMainMenuWorldDataInOrder();

            PlanetNodeList.gameObject.SetActive(false);

            MainCamera = Camera.main;
            OrbitOrbitCamera = MainCamera.GetComponent<PlanetOrbitCamera>();
        }

        void Start()
        {
            StartCameraPosition = MainCamera.transform.position;
            StartCameraRotation = MainCamera.transform.rotation;

            if (GameManager.Instance.GetGameResultState() == GameResultState.GameFinished)
                EnterLevelSelect();
            else
                EnterMainMenu();
        }

        void Update()
        {
            if (_isTransitioning) return;

            if (Input.GetKeyDown(KeyCode.Escape))
                PopPanel();
        }


        #region ----- 基础面板事件 ------------------------------

        private void InitScreenPanelDict()
        {
            void AddPanel(UIMainPanelType type)
            {
                if (_canvasScreenSpace.Find(type.ToString()).TryGetComponent<AbstractBasePanel>(out AbstractBasePanel panel))
                    _panelDict.Add(type, panel);
            }

            AddPanel(UIMainPanelType.MainMenuPanel);
            AddPanel(UIMainPanelType.LevelSelectPanel);
            AddPanel(UIMainPanelType.LevelDetailPanel);
            AddPanel(UIMainPanelType.PlayerConfigPanel);
            // AddPanel(UIMainPanelType.SettingsPanel);
            // AddPanel(UIMainPanelType.LoadGamePanel);

            foreach (var item in _panelDict.Values)
            {
                item.OnInit();
            }
        }

        private void InitWorldPanelDict()
        {
            PlanetNodeList = _canvasWorldSpace.Find("PlanetNodeList").GetComponent<PlanetNodeList>();
        }

        private void InitGameWorld()
        {
            PlanetGenerator = GameObject.Find("Planet").GetComponent<PlanetGenerator>();
        }

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

            _nodeS = PlanetNodeList.InitNodes(PlanetGenerator);
        }

        #endregion

        #region ----- 导航栈 ------------------------------

        /// <summary>
        /// 入栈一组 Panel，只显示主面板，其余叠加显示
        /// </summary>
        private void PushGroup(UIMainPanelGroup group)
        {
            if (group.Panels.Count == 0 || _isTransitioning) return;

            bool animate = _panelStack.Count > 0
                && ShouldAnimate(_panelStack.Peek().Panels[0], group.Panels[0]);

            if (!animate)
            {
                _panelStack.Push(group);
                ApplyPanelGroup(group.Panels);
                return;
            }

            _currentSequence?.Kill();
            _currentSequence = DOTween.Sequence();
            _isTransitioning = true;

            // 1) 淡出当前主面板
            var currentPrimary = _panelStack.Peek().Panels[0];
            var cg = GetPanelCanvasGroup(currentPrimary);
            if (cg != null)
                _currentSequence.Append(cg.DOFade(0f, _panelFadeDuration).SetEase(_fadeEase));

            // 2) 摄像机移动（DOLookAt 保证移动过程中自然注视目标）
            Vector3 targetPos = GetCameraTarget(group.Panels[0]);
            Vector3 lookTarget = GetLookAtTarget();
            _currentSequence.Append(MainCamera.transform.DOMove(targetPos, _cameraTransitionDuration).SetEase(_cameraEase));
            _currentSequence.Join(MainCamera.transform.DOLookAt(lookTarget, _cameraTransitionDuration));

            // 3) 完成回调
            _currentSequence.OnComplete(() =>
            {
                // 同步轨道状态（避免 ApplyOrbit 回弹）
                if (group.Panels[0] == UIMainPanelType.LevelSelectPanel)
                    OrbitOrbitCamera?.SyncFromPosition();

                _panelStack.Push(group);
                ApplyPanelGroup(group.Panels);

                _isTransitioning = false;
            });
        }

        /// <summary>
        /// 入栈单个 Panel（便捷方法）
        /// </summary>
        private void PushPanel(UIMainPanelType type)
        {
            PushGroup(new UIMainPanelGroup(type));
        }

        /// <summary>
        /// 出栈，回到上一组（保留栈底不空）
        /// </summary>
        private void PopPanel()
        {
            if (_panelStack.Count <= 1 || _isTransitioning) return;

            bool animate = ShouldAnimate(
                _panelStack.Peek().Panels[0],
                _panelStack.ToArray()[1].Panels[0]); // ≈ 栈底

            if (!animate)
            {
                _panelStack.Pop();
                ApplyPanelGroup(_panelStack.Peek().Panels);
                return;
            }

            _currentSequence?.Kill();
            _currentSequence = DOTween.Sequence();
            _isTransitioning = true;

            var currentGroup = _panelStack.Pop();   // ← 当前页
            var prevGroup    = _panelStack.Peek();   // ← 目标页
            var prevPrimary  = prevGroup.Panels[0];

            // 0) 先锁住摄像机
            _currentSequence.AppendCallback(() => LockCamera());

            // 1) 淡出当前主面板
            var cg = GetPanelCanvasGroup(currentGroup.Panels[0]);
            if (cg != null)
                _currentSequence.Append(cg.DOFade(0f, _panelFadeDuration).SetEase(_fadeEase));

            // 2) 摄像机移回目标位置
            Vector3 targetPos = GetCameraTarget(prevPrimary);
            Vector3 lookTarget = GetLookAtTarget();
            _currentSequence.Append(MainCamera.transform.DOMove(targetPos, _cameraTransitionDuration).SetEase(_cameraEase));
            _currentSequence.Join(MainCamera.transform.DOLookAt(lookTarget, _cameraTransitionDuration));

            // 3) 完成回调
            _currentSequence.OnComplete(() =>
            {
                if (prevPrimary == UIMainPanelType.LevelSelectPanel)
                    OrbitOrbitCamera?.SyncFromPosition();

                ApplyPanelGroup(prevGroup.Panels);

                _isTransitioning = false;
            });
        }

        #endregion

        #region ----- 面板显隐控制 ------------------------------

        private void ShowPanelOnly(UIMainPanelType type)
        {
            HideAll();
            var panel = GetOrCreatePanel(type);
            if (panel == null) return;

            panel.Show();
            UITool.ForceRebuildFormRoot(panel.GetComponent<RectTransform>());

            // 切回时重置 CanvasGroup alpha（可能被过渡动画淡出过）
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }

        private void ShowPanel(UIMainPanelType type)
        {
            if (_panelDict.TryGetValue(type, out var panel))
            {
                panel.Show();
                UITool.ForceRebuildFormRoot(panel.GetComponent<RectTransform>());
            }
        }

        private void HidePanel(UIMainPanelType type)
        {
            if (_panelDict.TryGetValue(type, out var panel))
                panel.Hide();
        }

        private void HideAll()
        {
            foreach (var panel in _panelDict.Values)
                panel.Hide();
        }

        private AbstractBasePanel GetOrCreatePanel(UIMainPanelType type)
        {
            if (!_panelDict.TryGetValue(type, out var panel))
            {
                string path = $"UIPanels/{type}";
                var prefab = Resources.Load<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogError($"找不到面板预制体: {path}");
                    return null;
                }
                var go = Instantiate(prefab, _canvasScreenSpace);
                panel = go.GetComponent<AbstractBasePanel>();
                _panelDict[type] = panel;
            }
            return panel;
        }

        public T GetPanel<T>(UIMainPanelType type) where T : AbstractBasePanel
        {
            return _panelDict.TryGetValue(type, out var panel) ? panel as T : null;
        }

        #endregion

        #region ----- 面板副作用处理 ------------------------------

        private void OnPanelGroupShow(List<UIMainPanelType> panels)
        {
            var primary = panels[0];
            switch (primary)
            {
                case UIMainPanelType.MainMenuPanel:
                    LockCamera();
                    ResetCamera();
                    PlanetNodeList.gameObject.SetActive(false);
                    break;

                case UIMainPanelType.LevelSelectPanel:
                    UnlockCamera();
                    PlanetNodeList.gameObject.SetActive(true);
                    break;

                case UIMainPanelType.LevelDetailPanel:
                    LockCamera();
                    break;
            }
        }

        #endregion

        #region ----- 镜头过渡辅助 ------------------------------

        /// <summary>
        /// 应用面板组：显示 + 副作用（动画完成后的最终步骤）
        /// </summary>
        private void ApplyPanelGroup(List<UIMainPanelType> panels)
        {
            ShowPanelOnly(panels[0]);
            for (int i = 1; i < panels.Count; i++)
                ShowPanel(panels[i]);
            OnPanelGroupShow(panels);
        }

        private static readonly HashSet<UIMainPanelType> _animatablePanels = new()
        {
            UIMainPanelType.MainMenuPanel,
            UIMainPanelType.LevelSelectPanel,
        };

        /// <summary>
        /// 仅当 from/to 都在可动画面板集合中时才播放过渡
        /// </summary>
        private static bool ShouldAnimate(UIMainPanelType from, UIMainPanelType to)
        {
            return _animatablePanels.Contains(from) && _animatablePanels.Contains(to);
        }

        private CanvasGroup GetPanelCanvasGroup(UIMainPanelType type)
        {
            if (_panelDict.TryGetValue(type, out var panel))
                return panel.GetComponent<CanvasGroup>();
            return null;
        }

        private Vector3 GetCameraTarget(UIMainPanelType panelType)
        {
            switch (panelType)
            {
                case UIMainPanelType.MainMenuPanel:
                    return StartCameraPosition;

                case UIMainPanelType.LevelSelectPanel:
                    return CalculateOrbitPosition(_levelSelectDirection);

                default:
                    return MainCamera.transform.position;
            }
        }

        private Vector3 CalculateOrbitPosition(Vector3 direction)
        {
            if (OrbitOrbitCamera == null || OrbitOrbitCamera.planetCenter == null)
                return MainCamera.transform.position;

            Vector3 dir = direction.normalized;
            float yaw   = Mathf.Atan2(dir.x, -dir.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, OrbitOrbitCamera.pitchRange.x, OrbitOrbitCamera.pitchRange.y);

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            return OrbitOrbitCamera.planetCenter.position
                 + rot * Vector3.back * OrbitOrbitCamera.orbitRadius;
        }

        private Vector3 GetLookAtTarget()
        {
            return OrbitOrbitCamera != null && OrbitOrbitCamera.planetCenter != null
                ? OrbitOrbitCamera.planetCenter.position
                : MainCamera.transform.position + MainCamera.transform.forward * 10f;
        }

        #endregion

        #region ----- 导航入口 ------------------------------

        public void EnterMainMenu()
        {
            PushPanel(UIMainPanelType.MainMenuPanel);
        }

        public void EnterLevelSelect()
        {
            if (_levelSystem.LevelDataCache.Count > 0)
            {
                _nodeS = PlanetNodeList.GenerateFromLevelOrderAndContinue(
                    PlanetGenerator,
                    _levelSystem.LevelDataCache,
                    4,
                    10f);
            }
            else
            {
                _nodeS = PlanetNodeList.InitNodes(PlanetGenerator);
            }

            PushPanel(UIMainPanelType.LevelSelectPanel);
        }

        public void EnterLevelConfirm()
        {
            PushGroup(new UIMainPanelGroup(
                UIMainPanelType.LevelDetailPanel,
                UIMainPanelType.PlayerConfigPanel));
        }

        #endregion

        #region ----- 其他 ------------------------------

        public void ResetCamera()
        {
            MainCamera.transform.SetPositionAndRotation(StartCameraPosition, StartCameraRotation);
        }

        public void LockCamera()
        {
            OrbitOrbitCamera.IsLock = true;
        }

        public void UnlockCamera()
        {
            OrbitOrbitCamera.ApplyOrbit();
            OrbitOrbitCamera.IsLock = false;
        }

        #endregion
    }
}
