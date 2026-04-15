using System;
using System.Collections.Generic;
using QFramework.Model;
using QFramework.Utility;
using QFramework.UtilityKit;
using QFramework.ViewController.Player;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QFramework.Manager
{
    public enum GameState
    {
        // 主菜单
        MainMenu,

        // 游戏中
        InitGame,
        Running,
        Pause,

        // 游戏结束
        GameOver,
    }

    public class GameManager : MonoSingleton<GameManager>, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        // ----- 获取架构层 ------------------------------
        private IResourceLoad _resourceLoad => this.GetUtility<IResourceLoad>();

        // ----- 状态机 ------------------------------
        private Dictionary<GameState, Action> _onEnterDict = new();
        private Dictionary<GameState, Action> _onExitDict = new();
        [SerializeField] private GameState _currentGameState;

        // ----- 主要节点 ------------------------------
        private Transform _manager;
        private Transform _scene;
        private Transform _ui;

        // ----- 脚本 ------------------------------
        private MapGenerator _map;
        private PlayerController _player;
        private CameraController _camera;

        protected override void Awake()
        {
            base.Awake();

            InitState();

           
        }

        void Start()
        {
            if(SceneManager.GetActiveScene().name == "Main")
            {
                ChangeState(GameState.MainMenu);
            }
            else
            {
                ChangeState(GameState.InitGame);
            }


        }

        // ----- 初始化状态机 ------------------------------
        private void InitState()
        {
            _onEnterDict[GameState.MainMenu] = OnEnterMainMenu;
            _onEnterDict[GameState.InitGame]     = OnEnterInitGame;
            _onEnterDict[GameState.Running]  = OnEnterRunning;
            _onEnterDict[GameState.Pause]    = OnEnterPause;
            _onEnterDict[GameState.GameOver] = OnEnterGameOver;

            _onExitDict[GameState.MainMenu]  = OnExitMainMenu;
            _onExitDict[GameState.InitGame]      = OnExitInitGame;
            _onExitDict[GameState.Running]   = OnExitRunning;
            _onExitDict[GameState.Pause]     = OnExitPause;
            _onExitDict[GameState.GameOver]  = OnExitGameOver;
        }

        // ----- 状态切换 ------------------------------
        public void ChangeState(GameState newState)
        {
            if (_currentGameState == newState) return;

            if (_onExitDict.TryGetValue(_currentGameState, out var onExit))
                onExit?.Invoke();

            _currentGameState = newState;

            if (_onEnterDict.TryGetValue(_currentGameState, out var onEnter))
                onEnter?.Invoke();
        }

        // ----- 各状态进入/退出回调 ------------------------------

        // ----- 主菜单回调 ------------------------------        
        private void OnEnterMainMenu() { /* 显示主菜单 UI */ }
        private void OnExitMainMenu()  { /* 隐藏主菜单 UI */ }

        // ----- 初始化回调 ------------------------------
        private void OnEnterInitGame()
        {
            InitGameScene();

            // 初始化地图、玩家等
            ChangeState(GameState.Running); // 初始化完直接进 Running
        }
        private void OnExitInitGame() { }

        // ----- 运行回调 ------------------------------

        private void OnEnterRunning() { Time.timeScale = 1f; }
        private void OnExitRunning()  { }

        // ----- 暂停回调 ------------------------------

        private void OnEnterPause()   { Time.timeScale = 0f; }
        private void OnExitPause()    { Time.timeScale = 1f; }

        // ----- 游戏结束回调 ------------------------------

        private void OnEnterGameOver() { /* 显示结算 UI */ }
        private void OnExitGameOver()  { }

        private void InitGameScene()
        {
            _manager = GameObject.Find("----------Manager----------").transform;
            _scene   = GameObject.Find("----------Scene----------").transform;
            _ui      = GameObject.Find("----------UI----------").transform;

            var map = _resourceLoad.Load<GameObject>("Prefab/Component/MapGenerator");
            _map = Instantiate(map, _scene).GetComponent<MapGenerator>();

            var player = _resourceLoad.Load<GameObject>("Prefab/Player/Player");
            _player = Instantiate(player, _scene).GetComponent<PlayerController>();    

            var camera = _resourceLoad.Load<GameObject>("Prefab/Player/MainCamera");
            _camera = Instantiate(camera, _scene).GetComponent<CameraController>();
        
            var playerPos = _player.transform.position;

            _camera.gameObject.transform.position = 
                new Vector3(playerPos.x, playerPos.y, -5);
            _camera.InitCameraTarget(_player.transform);

            // 加载地图数据
            _map.GenerateMapByLoadAsset(TerrainType.Plain);        
        }
    }
}