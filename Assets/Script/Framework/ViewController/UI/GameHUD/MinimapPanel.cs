using System.Collections.Generic;
using System.Linq;
using QFramework;
using QFramework.System;
using QFramework.ViewController.Enemy;
using QFramework.ViewController.Mission;
using QFramework.ViewController.Player;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class MinimapPanel : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        [Header("显示参数")]
        [SerializeField] private float _worldRange = 50f;
        [SerializeField] private float _mapSize = 150f;
        [SerializeField] private float _updateInterval = 0.5f;

        [Header("图标颜色")]
        [SerializeField] private Color _playerColor = Color.white;
        [SerializeField] private Color _enemyColor = Color.red;
        [SerializeField] private Color _beaconColor = Color.yellow;
        [SerializeField] private Color _extractionColor = Color.green;

        [Header("图标大小")]
        [SerializeField] private float _playerIconSize = 8f;
        [SerializeField] private float _enemyIconSize = 5f;
        [SerializeField] private float _missionIconSize = 6f;

        [Header("UI 引用")]
        [SerializeField] private RectTransform _iconRoot;
        private Sprite _iconSprite;

        // 图标对象
        private Image _playerIcon;
        private Image _beaconIcon;
        private Image _extractionIcon;
        private Dictionary<AbstractEnemy, Image> _enemyIcons = new();
        private List<AbstractEnemy> _activeEnemies = new();

        [Header("任务标题")]
        [SerializeField] private Text _missionTxt;

        // 追踪引用
        private Transform _playerTr;
        private Transform _beaconTr;
        private Transform _extractionTr;

        private float _timer;

        private void Awake()
        {
            _iconSprite = GenerateWhiteSprite();
        }

        private void Update()
        {
            UpdateMissionTime();

            if (_iconRoot == null) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = _updateInterval;
                RefreshAll();
            }
        }

        private void UpdateMissionTime()
        {
            if (_missionTxt == null) return;
            var stats = this.GetSystem<IStatsSystem>();
            int totalSec = Mathf.FloorToInt(stats.GameTimeSeconds);
            int h = totalSec / 3600;
            int m = (totalSec % 3600) / 60;
            int s = totalSec % 60;
            _missionTxt.text = $"// MINIMAP - {h:D2}:{m:D2}:{s:D2}";
        }

        private void RefreshAll()
        {
            // 玩家延迟获取
            if (_playerTr == null)
            {
                var player = PlayerController.Instance;
                if (player != null)
                {
                    _playerTr = player.transform;
                    _playerIcon = CreateIcon(_playerColor, _playerIconSize);
                    _playerIcon.rectTransform.anchoredPosition = Vector2.zero;
                }
            }

            // 任务点延迟获取
            if (_beaconTr == null)
            {
                var beacon = FindObjectOfType<BeaconMissionInstance>();
                _beaconTr = beacon?.transform;
                if (_beaconTr != null && _beaconIcon == null)
                    _beaconIcon = CreateIcon(_beaconColor, _missionIconSize);
            }
            if (_extractionTr == null)
            {
                var extraction = FindObjectOfType<ExtractionMissionInstance>();
                _extractionTr = extraction?.transform;
                if (_extractionTr != null && _extractionIcon == null)
                    _extractionIcon = CreateIcon(_extractionColor, _missionIconSize);
            }

            // 玩家未就绪时跳过坐标更新
            if (_playerTr == null) return;

            UpdateEnemies();
            UpdateIconPosition(_beaconIcon, _beaconTr);
            UpdateIconPosition(_extractionIcon, _extractionTr);
        }

        private void UpdateEnemies()
        {
            var active = TArmorArchitecture.Interface.GetSystem<IEnemyInstanceSystem>().GetActiveInstances().ToList();

            // 移除已不存在的敌人图标
            foreach (var kv in _enemyIcons.ToList())
            {
                if (!active.Contains(kv.Key))
                {
                    Destroy(kv.Value.gameObject);
                    _enemyIcons.Remove(kv.Key);
                }
            }

            // 新增敌人图标
            foreach (var enemy in active)
            {
                if (!_enemyIcons.ContainsKey(enemy))
                {
                    var icon = CreateIcon(_enemyColor, _enemyIconSize);
                    _enemyIcons[enemy] = icon;
                }
            }

            // 更新所有敌人位置
            foreach (var kv in _enemyIcons)
            {
                if (kv.Key != null)
                    UpdateIconPosition(kv.Value, kv.Key.transform);
            }
        }

        private void UpdateIconPosition(Image icon, Transform target)
        {
            if (icon == null || target == null) return;

            Vector3 offset = target.position - _playerTr.position;
            Vector2 normalized = new Vector2(offset.x, offset.y) / _worldRange;
            if (normalized.sqrMagnitude > 1f)
                normalized = normalized.normalized;

            icon.rectTransform.anchoredPosition = normalized * (_mapSize * 0.5f);
        }

        private Image CreateIcon(Color color, float size)
        {
            var go = new GameObject("MinimapIcon", typeof(RectTransform));
            go.transform.SetParent(_iconRoot, false);

            var img = go.AddComponent<Image>();
            img.sprite = _iconSprite;
            img.color = color;
            img.raycastTarget = false;

            var rt = img.rectTransform;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
            rt.anchorMin = Vector2.one * 0.5f;
            rt.anchorMax = Vector2.one * 0.5f;

            return img;
        }

        private static Sprite GenerateWhiteSprite()
        {
            var tex = new Texture2D(4, 4);
            var pixels = new Color32[16];
            for (int i = 0; i < 16; i++)
                pixels[i] = Color.white;
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
        }
    }
}
