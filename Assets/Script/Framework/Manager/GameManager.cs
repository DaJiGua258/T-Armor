using System;
using System.Collections;
using Framework.ViewController.UI;
using QFramework.Event;
using QFramework.System;
using QFramework.Utility;
using QFramework.UtilityKit;
using QFramework.ViewController.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QFramework.Manager
{
    public enum RuntimeGameState
    {
        None,
        Playing,
        Paused,
    }

    public enum GameResultState
    {
        None,
        GameOver,
        GameFinished,
    }

    public class GameManager : MonoSingleton<GameManager>, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        // ----- 场景名称 ------------------------------
        private const string MainSceneName = "Main";
        private const string GameSceneName = "Game";

        // ----- 场景节点名称 ------------------------------
        private const string ManagerRootName = "----------Manager----------";
        private const string SceneRootName = "----------Scene----------";
        private const string UIRootName = "----------UI----------";

        // ----- 预制体路径 ------------------------------
        private const string MapGeneratorPrefabPath = "Prefab/Component/MapGenerator";
        private const string PlayerPrefabPath = "Prefab/Player/Player";
        private const string CameraPrefabPath = "Prefab/Player/MainCamera";

        // ----- 获取层级 ------------------------------
        private IResourceLoad _resourceLoad => this.GetUtility<IResourceLoad>();
        private ILevelSystem _levelSystem => this.GetSystem<ILevelSystem>();

        // ----- 运行时状态 ------------------------------
        [SerializeField] private RuntimeGameState _runtimeState = RuntimeGameState.None;
        [SerializeField] private GameResultState _gameResultState = GameResultState.None; // 仅用于读取的游戏结果标识

        private Transform _manager;
        private Transform _scene;
        private Transform _ui;

        private MapGenerator _map;
        private PlayerController _player;
        private CameraController _camera;


        #region ----- 生命周期 ------------------------------
        protected override void Awake()
        {
            base.Awake();
        }

        void Start()
        {
            if (SceneManager.GetActiveScene().name == MainSceneName)
            {
                EnterMainScene();
            }
            else
            {
                EnterGameScene();
            }
        }
        #endregion

        #region ----- 对外 API ------------------------------
        /// <summary>
        /// 进入主场景
        /// </summary>
        public void EnterMainScene()
        {
            StartMainSceneFlow();
        }

        /// <summary>
        /// 进入游戏场景
        /// </summary>
        public void EnterGameScene()
        {
            StartGameSceneFlow();
        }

        /// <summary>
        /// 设置运行时状态
        /// </summary>
        /// <param name="newState"></param>
        public void SetRuntimeState(RuntimeGameState newState)
        {
            ChangeRuntimeState(newState);
        }
        
        public GameResultState GetGameResultState()
        {
            return _gameResultState;
        }

        public void SetGameResultState(GameResultState gameResultState)
        {
            _gameResultState = gameResultState;
        }

        public PlayerController Player => _player;
        #endregion

        #region ----- 场景流程 ------------------------------
        /// <summary>
        /// 开始主场景流程
        /// </summary>
        private void StartMainSceneFlow()
        {
            SendLoadingBegin();

            if (SceneManager.GetActiveScene().name != MainSceneName)
            {
                StartCoroutine(LoadSceneWithProgress(MainSceneName, OnMainSceneReady));
                return;
            }

            OnMainSceneReady();
        }

        /// <summary>
        /// 开始游戏场景流程
        /// </summary>
        private void StartGameSceneFlow()
        {
            SendLoadingBegin();

            if (SceneManager.GetActiveScene().name != GameSceneName)
            {
                StartCoroutine(LoadSceneWithProgress(GameSceneName, OnGameSceneReady));
                return;
            }

            OnGameSceneReady();
        }

        /// <summary>
        /// 主场景准备完成
        /// </summary>
        private void OnMainSceneReady()
        {
            InitMainScene();
            SendLoadingComplete(1f);
            _runtimeState = RuntimeGameState.None;
        }

        private void OnGameSceneReady()
        {
            // TODO: 恢复初始化
            if (!InitGameScene())
            {
                Debug.LogError("[GameManager] Init game scene failed, stop entering runtime state.");
                return;
            }

            SendLoadingComplete(1f);
            SetGameResultState(GameResultState.None);
            SetRuntimeState(RuntimeGameState.Playing);
        }
        #endregion

        #region ----- 运行态 ------------------------------
        /// <summary>
        /// 当前游戏场景的运行状态
        /// </summary>
        private void ChangeRuntimeState(RuntimeGameState newState)
        {
            if (_runtimeState == newState) return;
            
            // 处理旧状态的【退出逻辑】
            switch (_runtimeState)
            {
                case RuntimeGameState.Paused:
                    Time.timeScale = 1f;
                    break;
            }

            _runtimeState = newState;

            // 处理新状态的【进入逻辑】
            switch (_runtimeState)
            {
                case RuntimeGameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case RuntimeGameState.Paused:
                    Time.timeScale = 0f;
                    break;
            }
        }
        #endregion

        #region ----- 加载辅助 ------------------------------
        private void SendLoadingBegin()
        {
            TypeEventSystem.Global.Send(new LoadingEvent.ProgressTo() { Progress = 0f });
            TypeEventSystem.Global.Send(new LoadingEvent.FadeIn() { Duration = 0f });
        }

        private void SendLoadingComplete(float fadeDuration)
        {
            TypeEventSystem.Global.Send(new LoadingEvent.ProgressTo() { Progress = 1f });
            TypeEventSystem.Global.Send(new LoadingEvent.FadeOut() { Duration = fadeDuration });
        }

        /// <summary>
        /// 场景异步加载
        /// </summary>
        private IEnumerator LoadSceneWithProgress(string sceneName, Action onComplete)
        {
            yield return new WaitForSeconds(0.5f);
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            int step = 0;
            while (!asyncLoad.isDone || step < 4)
            {
                step++;
                float progress = Mathf.Clamp01(step / 4f);
                TypeEventSystem.Global.Send<LoadingEvent.ProgressTo>(new LoadingEvent.ProgressTo() { Progress = progress });
                yield return new WaitForSeconds(0.1f);
            }   
            onComplete?.Invoke();
        }
        #endregion
    
        #region ----- 具体实现 ------------------------------
        private bool InitGameScene()
        {
            if (!TryResolveSceneRoots()) return false;

            var mapPrefab = _resourceLoad.Load<GameObject>(MapGeneratorPrefabPath);
            if (mapPrefab == null)
            {
                Debug.LogError($"[GameManager] Can not load prefab: {MapGeneratorPrefabPath}");
                return false;
            }
            _map = Instantiate(mapPrefab, _scene).GetComponent<MapGenerator>();

            var playerPrefab = _resourceLoad.Load<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
            {
                Debug.LogError($"[GameManager] Can not load prefab: {PlayerPrefabPath}");
                return false;
            }
            _player = Instantiate(playerPrefab, _scene).GetComponent<PlayerController>();

            var cameraPrefab = _resourceLoad.Load<GameObject>(CameraPrefabPath);
            if (cameraPrefab == null)
            {
                Debug.LogError($"[GameManager] Can not load prefab: {CameraPrefabPath}");
                return false;
            }
            _camera = Instantiate(cameraPrefab, _scene).GetComponent<CameraController>();

            if (_map == null || _player == null || _camera == null)
            {
                Debug.LogError("[GameManager] Missing component on instantiated prefabs.");
                return false;
            }

            _map.GenerateMapByLoadAsset(_levelSystem.LoadedLevelData);

            // 读取 Entry 位置，传送玩家并初始化组件
            var missionSystem = this.GetSystem<IMissionSystem>();
            if (missionSystem.EntrySpawnPosition.HasValue)
            {
                var spawnPos = missionSystem.EntrySpawnPosition.Value;
                _player.transform.position = new Vector3(spawnPos.x, spawnPos.y, 0f);
                _camera.transform.position = new Vector3(spawnPos.x, spawnPos.y, -5f);
                _camera.InitCameraTarget(_player.transform);
            }
            else
            {
                _camera.InitCameraTarget(_player.transform);
            }

            return true;
        }

        private bool TryResolveSceneRoots()
        {
            _manager = FindSceneNode(ManagerRootName);
            _scene = FindSceneNode(SceneRootName);
            _ui = FindSceneNode(UIRootName);
            return _manager != null && _scene != null && _ui != null;
        }

        private static Transform FindSceneNode(string nodeName)
        {
            var node = GameObject.Find(nodeName);
            if (node == null)
            {
                Debug.LogError($"[GameManager] Scene node not found: {nodeName}");
                return null;
            }

            return node.transform;
        }

        private void InitMainScene()
        { 

        }
        #endregion
    }
}