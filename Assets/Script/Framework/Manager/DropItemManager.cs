using System.Collections;
using System.Collections.Generic;
using QFramework.Event;
using QFramework.Enum;
using QFramework.Utility;
using QFramework.UtilityKit;
using QFramework.ViewController.Enemy;
using UnityEngine;
using System;

namespace QFramework.Manager
{
    [Serializable]
    public class EnemyDropConfig
    {
        public EnemyTypeEnum enemyType;
        [Range(0f, 1f)] public float dropProbability = 0.5f;
    }

    [Serializable]
    public class DropItemEntry
    {
        public ItemTypeEnum itemType;
        [Range(1, 100)] public int weight = 1;
    }

    public class DropItemManager : SceneMonoSingleton<DropItemManager>, IController
    {
        [Header("敌人掉落概率")]
        [SerializeField] private List<EnemyDropConfig> _enemyConfigs = new();

        [Header("掉落表（仅 Mod 类型）")]
        [SerializeField] private List<DropItemEntry> _dropTable = new();

        [Header("掉落物生成")]
        [SerializeField] private GameObject _dropItemPrefab;
        [SerializeField] private float _dropDelay = 5f;
        [SerializeField] private float _dropRadius = 0.5f;

        protected override void Awake()
        {
            base.Awake();
            TypeEventSystem.Global.Register<StatsEvent.OnEnemyKilled>(OnEnemyKilled);
        }

        private void OnDestroy()
        {
            TypeEventSystem.Global.UnRegister<StatsEvent.OnEnemyKilled>(OnEnemyKilled);
        }

        private void OnEnemyKilled(StatsEvent.OnEnemyKilled evt)
        {
            var config = _enemyConfigs.Find(c => c.enemyType == evt.Type);
            if (config == null) return;
            if (UnityEngine.Random.value > config.dropProbability) return;

            var enemy = FindEnemyById(evt.EnemyId);
            if (enemy == null) return;

            StartCoroutine(DropRoutine(enemy.transform.position));
        }

        private IEnumerator DropRoutine(Vector3 position)
        {
            yield return new WaitForSeconds(_dropDelay);

            Vector3 offset = UnityEngine.Random.insideUnitCircle * _dropRadius;
            this.GetUtility<IObjectPoolUtility>().GetObject(
                _dropItemPrefab, position + offset, Quaternion.identity);
        }

        /// <summary>
        /// 玩家拾取时回调：权重随机结算掉落物品
        /// </summary>
        public ItemTypeEnum SettleDrop()
        {
            if (_dropTable == null || _dropTable.Count == 0)
                return ItemTypeEnum.None;

            int totalWeight = 0;
            foreach (var entry in _dropTable)
                totalWeight += entry.weight;

            int roll = UnityEngine.Random.Range(0, totalWeight);
            foreach (var entry in _dropTable)
            {
                roll -= entry.weight;
                if (roll < 0)
                    return entry.itemType;
            }

            return _dropTable[0].itemType;
        }

        private AbstractEnemy FindEnemyById(int enemyId)
        {
            var enemies = FindObjectsByType<AbstractEnemy>(FindObjectsSortMode.None);
            foreach (var e in enemies)
            {
                if (e.enemyId == enemyId)
                    return e;
            }
            return null;
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Print Drop Table")]
        private void DebugPrint()
        {
            Debug.Log($"=== DropItemManager ===");
            Debug.Log($"Prefab: {(_dropItemPrefab ? _dropItemPrefab.name : "null")}");
            Debug.Log($"Delay: {_dropDelay}s, Radius: {_dropRadius}");
            Debug.Log($"--- Enemy Configs ({_enemyConfigs.Count}) ---");
            foreach (var c in _enemyConfigs)
                Debug.Log($"  {c.enemyType} → {c.dropProbability:P0}");
            Debug.Log($"--- Drop Table ({_dropTable.Count}) ---");
            foreach (var e in _dropTable)
                Debug.Log($"  {e.itemType}  weight={e.weight}");
        }
#endif
    }
}
