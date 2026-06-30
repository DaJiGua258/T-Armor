using System.Collections.Generic;
using QFramework.Event;
using QFramework.ViewController.Enemy;
using UnityEngine;

namespace QFramework.ViewController.UI
{
    /// <summary>
    /// 伤害数字管理器。
    ///
    /// 【重要 Inspector 设置】
    /// _damageNumberRoot 上的 Canvas 必须设置为 World Space，
    /// 并调整 Canvas 的 Scale（通常设为 0.01），使字体大小与游戏世界比例匹配。
    /// </summary>
    public class DamageNumberManager : BaseUIComponent
    {
        [SerializeField] private GameObject _damageNumberPrefab;
        [SerializeField] private Transform  _damageNumberRoot;
        [SerializeField] private int        _preCreateCount = 15;

        [Header("世界坐标偏移")]
        [SerializeField] private float _baseWorldYOffset = 1.5f;

        [Header("弹出动画")]
        [SerializeField] private float _popUpOffset   = 0.5f;
        [SerializeField] private float _popUpDuration = 0.25f;

        [Header("停留")]
        [SerializeField] private float _displayDuration = 0.4f;

        [Header("淡出")]
        [SerializeField] private float _fadeOutDuration = 0.3f;

        [Header("随机偏移")]
        [SerializeField] private float _randomXOffset = 0.25f;
        [SerializeField] private float _randomYOffset = 0.25f;

        private readonly Stack<DamageNumber>           _pool       = new Stack<DamageNumber>();
        private readonly Dictionary<int, AbstractEnemy> _enemyCache = new Dictionary<int, AbstractEnemy>();

        // ──────────────────────────────────────────────
        // 生命周期
        // ──────────────────────────────────────────────

        private void Start()
        {
            if (_damageNumberPrefab == null || _damageNumberRoot == null)
            {
                Debug.LogError("[DamageNumberManager] 请在 Inspector 中赋值 _damageNumberPrefab 和 _damageNumberRoot");
                return;
            }

            for (int i = 0; i < _preCreateCount; i++)
                CreateAndPool();

            TypeEventSystem.Global.Register<StatsEvent.OnDamageDealt>(OnDamageDealt)
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            TypeEventSystem.Global.Register<StatsEvent.OnEnemyKilled>(OnEnemyKilled)
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            // 让场上已存在的敌人主动注册（避免首次伤害时触发全场搜索）
            RefreshEnemyCache();
        }

        private void OnDestroy()
        {
            _enemyCache.Clear();
        }

        // ──────────────────────────────────────────────
        // 对象池
        // ──────────────────────────────────────────────

        private DamageNumber CreateAndPool()
        {
            var go = Instantiate(_damageNumberPrefab, _damageNumberRoot);
            var dn = go.GetComponent<DamageNumber>();
            dn.OnAnimationComplete = () => Pool(dn);
            dn.ResetState();
            _pool.Push(dn);
            return dn;
        }

        private void Pool(DamageNumber dn)
        {
            dn.ResetState();
            _pool.Push(dn);   // ResetState 已保证 IsAvailable = true，无需再做重复检测
        }

        /// <summary>
        /// 修复：原实现 count 固定但会把不可用对象重新推回栈，
        /// 导致循环内反复弹出同一个不可用对象。
        /// 新实现：直接弹空直到找到可用对象，找不到就扩容。
        /// </summary>
        private DamageNumber Get()
        {
            while (_pool.Count > 0)
            {
                var dn = _pool.Pop();
                if (dn != null && dn.IsAvailable)
                    return dn;
                // null 或不可用的直接丢弃（不可用说明它仍在播动画，不应在池里，属于异常情况）
            }
            // 池空或全部不可用 → 动态扩容
            return CreateAndPool();
        }

        // ──────────────────────────────────────────────
        // 事件处理
        // ──────────────────────────────────────────────

        private void OnDamageDealt(StatsEvent.OnDamageDealt evt)
        {
            var enemy = FindEnemy(evt.EnemyId);
            if (enemy == null) return;

            Vector3 worldPos = enemy.transform.position + Vector3.up * _baseWorldYOffset;

            var dn = Get();
            dn.Show(evt.Damage, worldPos,
                _popUpOffset, _popUpDuration,
                _displayDuration, _fadeOutDuration,
                _randomXOffset, _randomYOffset);
        }

        private void OnEnemyKilled(StatsEvent.OnEnemyKilled evt)
        {
            _enemyCache.Remove(evt.EnemyId);
        }

        // ──────────────────────────────────────────────
        // 敌人缓存
        // ──────────────────────────────────────────────

        /// <summary>
        /// 供 AbstractEnemy 在 OnEnable/Start 时主动调用，
        /// 避免每次伤害事件都触发 FindObjectsByType 全场扫描。
        /// </summary>
        public void RegisterEnemy(AbstractEnemy enemy)
        {
            if (enemy != null)
                _enemyCache[enemy.enemyId] = enemy;
        }

        /// <summary>
        /// 供 AbstractEnemy 在 OnDisable/OnDestroy 时主动调用。
        /// </summary>
        public void UnregisterEnemy(AbstractEnemy enemy)
        {
            if (enemy != null)
                _enemyCache.Remove(enemy.enemyId);
        }

        private AbstractEnemy FindEnemy(int id)
        {
            if (_enemyCache.TryGetValue(id, out var enemy) && enemy != null)
                return enemy;

            // 缓存 miss：刷新一次后再查（降低全场搜索频率）
            RefreshEnemyCache();
            _enemyCache.TryGetValue(id, out var result);
            return result;
        }

        /// <summary>
        /// 全场扫描并刷新缓存（仅在缓存 miss 时调用）。
        /// </summary>
        private void RefreshEnemyCache()
        {
            var enemies = FindObjectsByType<AbstractEnemy>(FindObjectsSortMode.None);
            foreach (var e in enemies)
            {
                if (e != null)
                    _enemyCache[e.enemyId] = e;
            }
        }

        // ──────────────────────────────────────────────
        // 调试
        // ──────────────────────────────────────────────

        [ContextMenu("Test Damage Number")]
        private void TestDamageNumber()
        {
            var dn = Get();
            dn.Show(Random.Range(1, 999), Vector3.zero,
                _popUpOffset, _popUpDuration,
                _displayDuration, _fadeOutDuration,
                _randomXOffset, _randomYOffset);
        }
    }
}