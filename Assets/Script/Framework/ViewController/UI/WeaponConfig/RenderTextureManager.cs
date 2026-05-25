using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using QFramework;
using QFramework.Enum;
using QFramework.Model;
using QFramework.Utility;
using QFramework.ViewController.Player;

namespace QFramework.ViewController.UI.WeaponConfig
{
    public class RenderTextureManager : MonoBehaviour, IController
    {
        [Header("武器 RT 生成预制体")]
        [SerializeField] private GameObject _rtPrefab;

        [Header("RT 分辨率")]
        [SerializeField] private int _rtWidth = 256;
        [SerializeField] private int _rtHeight = 256;

        [Header("模型自转")]
        [SerializeField] private float _rotationSpeed = 30f;

        [Header("生成布局（相对 Weapons 节点）")]
        [SerializeField] private Vector3 _itemInterval;

        [Header("Player 预览")]
        [SerializeField] private GameObject _playerPrefab;
        [Header("Player RT 分辨率")]
        [SerializeField] private int _playerRTWidth = 512;
        [SerializeField] private int _playerRTHeight = 512;

        private const string WEAPONS_ROOT_NAME = "Weapons";
        private const string PLAYER_ROOT_NAME = "Player";
        private const string PREFAB_CHILD_NAME = "Prefab";

        public static RenderTextureManager Instance { get; private set; }

        private Dictionary<WeaponTypeEnum, RenderTexture> _rtMap = new();
        private List<GameObject> _generatedObjects = new();
        private List<Transform> _weaponModels = new();
        private RenderTexture _playerRT;
        private GameObject _playerModelInstance;
        private PlayerController _playerController;
        private Transform _weaponsRoot;
        private Transform _playerRoot;
        private Camera _playerCamera;

        public RenderTexture PlayerRT => _playerRT;

        private void Awake()
        {
            Instance = this;
            _weaponsRoot = transform.Find(WEAPONS_ROOT_NAME);
            _playerRoot = transform.Find(PLAYER_ROOT_NAME);
            if (_playerRoot != null)
                _playerCamera = _playerRoot.GetComponentInChildren<Camera>();
        }

        private void Start()
        {
            GenerateAll();
        }

        [ContextMenu("GenerateAll")]
        public void GenerateAll()
        {
            Cleanup();
            _rtMap.Clear();

            var model = this.GetModel<IWeaponConfigModel>();
            var types = model.WeaponConfigs.Keys
                .Union(model.HangerWeaponConfigs.Keys)
                .Where(t => t != WeaponTypeEnum.None)
                .Distinct()
                .ToList();

            var weaponParent = _weaponsRoot != null ? _weaponsRoot : transform;

            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];
                var isHanger = model.HangerWeaponConfigs.Keys.Contains(type);
                var path = isHanger ? $"Prefab/Weapon/Hanger/{type}" : $"Prefab/Weapon/Slot/{type}";
                var weaponPrefab = this.GetUtility<IResourceLoad>().Load<GameObject>(path);
                if (weaponPrefab == null) continue;

                var rtInstance = Instantiate(_rtPrefab, weaponParent);
                _generatedObjects.Add(rtInstance);

                var cam = rtInstance.GetComponentInChildren<Camera>();
                if (cam == null) continue;

                var prefabChild = rtInstance.transform.Find(PREFAB_CHILD_NAME);
                if (prefabChild != null)
                {
                    var weaponObj = Instantiate(weaponPrefab, prefabChild);
                    foreach (var mb in weaponObj.GetComponentsInChildren<MonoBehaviour>())
                        mb.enabled = false;

                    var mesh = weaponObj.transform.Find("Mesh");
                    if (mesh != null) _weaponModels.Add(mesh);
                    var shadow = weaponObj.transform.Find("Shadow");
                    if (shadow != null) _weaponModels.Add(shadow);
                }

                var rt = new RenderTexture(_rtWidth, _rtHeight, 16);
                rt.Create();
                cam.targetTexture = rt;

                _rtMap[type] = rt;
            }

            GeneratePlayerPreview();
            UpdatePositions();
        }

        private void GeneratePlayerPreview()
        {
            if (_playerPrefab == null || _playerRoot == null || _playerCamera == null) return;

            // 清除旧的 player 模型
            if (_playerModelInstance != null)
            {
                Destroy(_playerModelInstance);
                _playerModelInstance = null;
            }

            var prefabChild = _playerRoot.Find(PREFAB_CHILD_NAME);
            var parent = prefabChild != null ? prefabChild : _playerRoot;

            _playerModelInstance = Instantiate(_playerPrefab, parent);
            // foreach (var mb in _playerModelInstance.GetComponentsInChildren<MonoBehaviour>())
            //     mb.enabled = false;
            _playerController = _playerModelInstance.GetComponent<PlayerController>();
            _playerController.SetLockState(true);

            var rt = new RenderTexture(_playerRTWidth, _playerRTHeight, 16);
            rt.Create();
            _playerCamera.targetTexture = rt;
            _playerRT = rt;
        }

        [ContextMenu("UpdatePositions")]
        public void UpdatePositions()
        {
            for (int i = 0; i < _generatedObjects.Count; i++)
                _generatedObjects[i].transform.localPosition = _itemInterval * i;
        }

        private void Update()
        {
            var delta = _rotationSpeed * Time.deltaTime;

            foreach (var t in _weaponModels)
                t.Rotate(0f, 0, delta);

            if (_playerController != null)
                _playerController.RotatePreview(delta);
        }

        private void OnValidate()
        {
            UpdatePositions();
        }

        public RenderTexture GetRT(WeaponTypeEnum type)
        {
            _rtMap.TryGetValue(type, out var rt);
            return rt;
        }

        private void Cleanup()
        {
            foreach (var obj in _generatedObjects)
                Destroy(obj);
            _generatedObjects.Clear();
            _weaponModels.Clear();

            foreach (var rt in _rtMap.Values)
                rt.Release();
            _rtMap.Clear();

            if (_playerRT != null)
            {
                _playerRT.Release();
                _playerRT = null;
            }

            if (_playerModelInstance != null)
            {
                Destroy(_playerModelInstance);
                _playerModelInstance = null;
            }
            _playerController = null;

            if (_playerCamera != null)
                _playerCamera.targetTexture = null;
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
    }
}
