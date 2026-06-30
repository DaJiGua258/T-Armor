using System.Collections.Generic;
using DG.Tweening;
using QFramework.System;
using QFramework.Utility;
using QFramework.UtilityKit;
using QFramework.ViewController.UI;
using QFramework.ViewController.MainMenuUI;
using QFramework.Model;
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
        private bool _restoreOrbitPending;
        private bool _playMainMenuCameraAnimation;
        private Sequence _currentSequence;
        public UIMainPanelType CurrentPanel => _panelStack.Count > 0 ? _panelStack.Peek().Panels[0] : UIMainPanelType.MainMenuPanel;

        // ----- 世界空间面板 ------------------------------
        public PlanetNodeList PlanetNodeList;

        // ----- 十字准星 -------------------------------------
        private ScreenCrosshair _screenCrosshair;

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
        [SerializeField] private Ease _cameraEase = Ease.InOutQuad;
        [SerializeField] private Ease _fadeEase = Ease.OutQuad;
        [SerializeField] private Vector3 _levelSelectDirection = new Vector3(0f, 0.342f, -0.94f);


        protected override void Awake()
        {
            base.Awake();

            Canvas.ForceUpdateCanvases();

            _canvasWorldSpace = transform.Find("CanvasWorldSpace");
            _canvasScreenSpace = transform.Find("CanvasScreenSpace");

            _screenCrosshair = _canvasScreenSpace.GetComponentInChildren<ScreenCrosshair>(true);

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
            {
                // 相机定位到最后一个完成节点的法线方向（默认距离 2）
                var cache = _levelSystem.LevelDataCache;
                if (cache.Count > 0 && OrbitOrbitCamera?.planetCenter != null)
                {
                    Vector3 lastNormal = cache[^1].EnvironmentData.SurfaceNormal.normalized;
                    Vector3 camPos = OrbitOrbitCamera.planetCenter.position + lastNormal * 2f;
                    Quaternion camRot = Quaternion.LookRotation(
                        OrbitOrbitCamera.planetCenter.position - camPos, Vector3.up);
                    MainCamera.transform.SetPositionAndRotation(camPos, camRot);
                    StartCameraPosition = camPos;
                    StartCameraRotation = camRot;
                }

                _panelStack.Push(new UIMainPanelGroup(UIMainPanelType.MainMenuPanel));
                EnterLevelSelect();
            }
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

            // 2) 完成回调
            _currentSequence.OnComplete(() =>
            {
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
                var targetPrimary = _panelStack.Peek().Panels[0];
                if (targetPrimary == UIMainPanelType.LevelSelectPanel)
                    _restoreOrbitPending = true;
                ApplyPanelGroup(_panelStack.Peek().Panels);

                // 从 LevelDetail 回到 LevelSelect：沿法线回退，十字准星立即解锁
                if (targetPrimary == UIMainPanelType.LevelSelectPanel)
                {
                    _screenCrosshair?.Show();
                    _screenCrosshair?.Unlock();
                    PlanetNodeList?.ResetNodeHighlight();
                    OrbitOrbitCamera?.ReturnFromFocus(_cameraTransitionDuration, () =>
                    {
                        UnlockCamera();
                    });
                }
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

            // 2) 完成回调
            _currentSequence.OnComplete(() =>
            {
                // 从 LevelSelect 回到 MainMenu 时才播放相机过渡
                _playMainMenuCameraAnimation = currentGroup.Panels[0] == UIMainPanelType.LevelSelectPanel
                                            && prevPrimary == UIMainPanelType.MainMenuPanel;

                if (currentGroup.Panels[0] == UIMainPanelType.LevelSelectPanel)
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
                    if (_playMainMenuCameraAnimation && OrbitOrbitCamera != null && OrbitOrbitCamera.planetCenter != null)
                    {
                        _playMainMenuCameraAnimation = false;
                        MainCamera.transform.DOKill();

                        // 初始隐藏 MainMenu，Step 2 再淡入
                        var menuCG = GetPanelCanvasGroup(UIMainPanelType.MainMenuPanel);
                        if (menuCG != null) menuCG.alpha = 0f;

                        Vector3 defaultOrbitPos = OrbitOrbitCamera.GetDefaultOrbitPosition();
                        Quaternion defaultOrbitRot = Quaternion.LookRotation(
                            OrbitOrbitCamera.planetCenter.position - defaultOrbitPos, Vector3.up);

                        var cameraSeq = DOTween.Sequence();
                        // Step 1: 回到默认轨道位置（MainMenu 不可见）
                        cameraSeq.Append(MainCamera.transform.DOMove(defaultOrbitPos, _cameraTransitionDuration).SetEase(_cameraEase));
                        cameraSeq.Join(MainCamera.transform.DORotateQuaternion(defaultOrbitRot, _cameraTransitionDuration).SetEase(_cameraEase));

                        // Step 2: 回到初始菜单位置 + MainMenu 淡入
                        cameraSeq.Append(MainCamera.transform.DOMove(StartCameraPosition, _cameraTransitionDuration).SetEase(_cameraEase));
                        cameraSeq.Join(MainCamera.transform.DORotateQuaternion(StartCameraRotation, _cameraTransitionDuration).SetEase(_cameraEase));
                        if (menuCG != null)
                            cameraSeq.Join(menuCG.DOFade(1f, _cameraTransitionDuration).SetEase(_fadeEase));
                        cameraSeq.OnComplete(() => OrbitOrbitCamera.SyncFromPosition());
                    }
                    PlanetNodeList.gameObject.SetActive(false);
                    _screenCrosshair?.Hide();
                    break;

                case UIMainPanelType.LevelSelectPanel:
                    if (OrbitOrbitCamera != null && OrbitOrbitCamera.planetCenter != null)
                    {
                        if (_restoreOrbitPending)
                        {
                            _restoreOrbitPending = false;
                            // 从 LevelDetail 返回，由 ReturnFromFocus 处理相机动画，此处只恢复面板
                        }
                        else
                        {
                            // 初始隐藏 LevelSelect，相机到达默认轨道后淡入
                            var levelCG = GetPanelCanvasGroup(UIMainPanelType.LevelSelectPanel);
                            if (levelCG != null) levelCG.alpha = 0f;

                            Vector3 targetPos = OrbitOrbitCamera.GetDefaultOrbitPosition();

                            MainCamera.transform.DOKill();

                            Quaternion targetRot = Quaternion.LookRotation(
                                OrbitOrbitCamera.planetCenter.position - targetPos, Vector3.up);

                            var cameraSeq = DOTween.Sequence();
                            cameraSeq.Join(MainCamera.transform.DOMove(targetPos, _cameraTransitionDuration)
                                .From(StartCameraPosition)
                                .SetEase(_cameraEase));
                            cameraSeq.Join(MainCamera.transform.DORotateQuaternion(targetRot, _cameraTransitionDuration)
                                .SetEase(_cameraEase));
                            cameraSeq.OnComplete(() =>
                            {
                                if (levelCG != null)
                                    levelCG.DOFade(1f, _panelFadeDuration).SetEase(_fadeEase);
                                OrbitOrbitCamera.SyncFromPosition();
                                UnlockCamera();

                                _screenCrosshair?.Show();
                                _screenCrosshair?.Unlock();
                            });
                        }
                    }
                    else
                    {
                        UnlockCamera();
                        _screenCrosshair?.Show();
                        _screenCrosshair?.Unlock();
                    }
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
                    return OrbitOrbitCamera != null
                        ? OrbitOrbitCamera.GetOrbitPosition(_levelSelectDirection)
                        : MainCamera.transform.position;

                default:
                    return MainCamera.transform.position;
            }
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
            var storage = this.GetUtility<IStorageUtility>();

            if (_levelSystem.LevelDataCache.Count > 0)
            {
                // 检查是否有已存档的待选节点（游戏启动后首次进入）
                var saveData = storage.LoadData<GameSaveData>("GameSaveData");
                if (saveData != null && saveData.PendingNodes.Count > 0)
                {
                    // 从存档恢复完整星球（包括待选节点）
                    _nodeS = PlanetNodeList.RestoreFromSaveData(
                        PlanetGenerator,
                        _levelSystem.LevelDataCache,
                        saveData.PendingNodes);

                    // 清除存档中的待选节点，避免下次重复使用
                    saveData.PendingNodes.Clear();
                    storage.SaveData("GameSaveData", saveData);
                }
                else
                {
                    // 通关后返回，重新生成待选节点
                    _nodeS = PlanetNodeList.GenerateFromLevelOrderAndContinue(
                        PlanetGenerator,
                        _levelSystem.LevelDataCache,
                        3,
                        10f);

                    // 保存新生成的待选节点
                    SavePendingNodeData();
                }
            }
            else
            {
                _nodeS = PlanetNodeList.InitNodes(PlanetGenerator);
            }

            PushPanel(UIMainPanelType.LevelSelectPanel);
        }

        private void SavePendingNodeData()
        {
            var pendingNodes = PlanetNodeList.GetPendingNodesData();
            if (pendingNodes == null || pendingNodes.Count == 0) return;

            var storage = this.GetUtility<IStorageUtility>();
            var saveData = storage.LoadData<GameSaveData>("GameSaveData") ?? new GameSaveData();
            saveData.PendingNodes = pendingNodes;
            storage.SaveData("GameSaveData", saveData);
        }

        public void EnterLevelConfirm(Vector3 nodeWorldPosition)
        {
            // 立即锁定十字线到 node，后续镜头动画过程中持续追踪
            _screenCrosshair?.LockAtWorldPosition(nodeWorldPosition);

            if (OrbitOrbitCamera != null)
            {
                OrbitOrbitCamera.FocusOnNode(nodeWorldPosition, _cameraTransitionDuration);
            }
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
