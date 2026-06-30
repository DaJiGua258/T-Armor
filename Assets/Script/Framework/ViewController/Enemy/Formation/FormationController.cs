using System.Collections.Generic;
using Pathfinding;
using QFramework.Enum;
using UnityEngine;

namespace QFramework.ViewController.Enemy.Formation
{
    /// <summary>
    /// 队形控制器，挂载在父物体上。
    /// 按 SO 配置从内到外生成环形队形，leader 在中心，followers 保持偏移跟随。
    /// </summary>
    public class FormationController : MonoBehaviour
    {
        [Header("SO 配置")]
        [SerializeField] private FormationTypesSO _typesSO;  // 拖拽 SO 引用（用于保存/加载）

        [Header("队形配置（每种类型生成一个环，从内到外）")]
        public List<EnemyCount> Types = new List<EnemyCount>();  // 敌人种类与数量列表
        public float RingRadius = 3f;  // 最内环半径
        public float RingSpacing = 2f;  // 每层环半径递增步长

        [Header("生成设置")]
        public bool SpawnOnStart = true;  // 启动时自动生成队形

        private AbstractEnemy _leader;  // 队形 leader（列表第一个敌人）
        private readonly List<FollowerInfo> _followers = new List<FollowerInfo>();  // 跟随者列表

        /// <summary>运行时偏移数据</summary>
        private class FollowerInfo
        {
            public AbstractEnemy Enemy;  // 跟随者实例
            public Vector3 LocalOffset;  // 相对 leader 的偏移量
        }

        /// <summary>检查位置是否在寻路区域内</summary>
        private static bool IsOnNavMesh(Vector3 position, float maxDistance = 0.5f)
        {
            if (AstarPath.active == null) return false;
            var nn = AstarPath.active.GetNearest(position, NearestNodeConstraint.Walkable);
            return nn.node != null && nn.node.Walkable &&
                   (nn.position - position).sqrMagnitude <= maxDistance * maxDistance;
        }

        /// <summary>
        /// 从指定敌人身上获取有效的 target。
        /// 优先返回战斗状态中的目标，其次返回已持有但暂未交战的目标。
        /// </summary>
        private static Transform GetActiveTargetFrom(AbstractEnemy enemy)
        {
            if (enemy == null) return null;
            string state = enemy.GetCurrentState();
            if (state == "Attack" || state == "Move")
                return enemy.Target;
            if (enemy.Target != null && enemy.Target.gameObject.activeInHierarchy)
                return enemy.Target;
            return null;
        }

        #region ----- 生命周期 -------------------------

        private void Start()
        {
            if (SpawnOnStart) SpawnFormation();
        }

        /// <summary>
        /// 每帧驱动 follower 向队形目标位置移动。
        /// 不直接检测玩家，改为由子物体敌人各自的 FSM 自行检测，
        /// 一旦任一敌人发现目标（战斗状态或已持有 target），自动分发给全队。
        /// </summary>
        private void Update()
        {
            // leader 死亡则解散队形
            if (_leader == null || _leader.IsDead())
            {
                _followers.Clear();
                return;
            }

            // ----- 收集当前有效的 target（优先从战斗中取，其次从已持有 target 的取）-----
            Transform detectedTarget = GetActiveTargetFrom(_leader);

            if (detectedTarget == null)
            {
                foreach (var info in _followers)
                {
                    if (info.Enemy == null || info.Enemy.IsDead()) continue;
                    detectedTarget = GetActiveTargetFrom(info.Enemy);
                    if (detectedTarget != null) break;
                }
            }

            // ----- 有目标 → 通知尚未进入战斗的全队 -----
            if (detectedTarget != null && detectedTarget.gameObject.activeInHierarchy)
            {
                string leaderState = _leader.GetCurrentState();
                if (leaderState == "Idle" || leaderState == "Patrol")
                    _leader.GetTarget(detectedTarget);

                foreach (var info in _followers)
                {
                    if (info.Enemy == null || info.Enemy.IsDead()) continue;
                    string state = info.Enemy.GetCurrentState();
                    if (state == "Idle" || state == "Patrol")
                        info.Enemy.GetTarget(detectedTarget);
                }
            }

            // ----- 倒序遍历 follower：清理死亡 + 驱动非战斗单位保持队形 -----
            for (int i = _followers.Count - 1; i >= 0; i--)
            {
                var info = _followers[i];
                if (info.Enemy == null || info.Enemy.IsDead())
                {
                    _followers.RemoveAt(i);
                    continue;
                }

                // 战斗状态下由 FSM 自主控制，不覆盖移动
                string state = info.Enemy.GetCurrentState();
                if (state == "Attack" || state == "Move") continue;

                // 向目标队形位置移动
                Vector3 targetPos = _leader.transform.position + info.LocalOffset;
                if (!IsOnNavMesh(targetPos))
                    targetPos = _leader.transform.position;
                info.Enemy.StartPatrolMovement();
                info.Enemy.MoveToward(targetPos);
            }
        }

        #endregion

        #region ----- 生成与销毁 -------------------------

        /// <summary>
        /// 接收 SO 参数，加载其配置后生成队形
        /// </summary>
        public void SpawnFromSO(FormationTypesSO so)
        {
            if (so == null || so.Types == null || so.Types.Count == 0)
            {
                Debug.LogWarning("[FormationController] 传入的 SO 为空或没有敌人配置");
                return;
            }

            RingRadius = so.RingRadius;
            RingSpacing = so.RingSpacing;
            Types = new List<EnemyCount>(so.Types);
            SpawnFormation();
        }

        /// <summary>
        /// 按面板 Types 配置生成敌人并计算环形偏移
        /// </summary>
        [ContextMenu("生成队形")]
        public void SpawnFormation()
        {
            if (Types == null || Types.Count == 0)
            {
                Debug.LogWarning("[FormationController] Types 列表为空，请在面板中添加敌人配置");
                return;
            }

            ClearFormation();

            // 展平所有敌人实例
            var allEnemies = new List<AbstractEnemy>();
            foreach (var entry in Types)
            {
                if (entry == null || entry.Count <= 0) continue;
                for (int i = 0; i < entry.Count; i++)
                {
                    var enemy = SpawnSingle(entry.EnemyType);
                    if (enemy != null) allEnemies.Add(enemy);
                }
            }

            if (allEnemies.Count == 0) return;

            _leader = allEnemies[0];
            _followers.Clear();

            // 按 Types 顺序从内到外生成环，每种类别一个环
            int absIndex = 1;  // allEnemies 索引，0 是 leader
            int ringIdx = 0;  // 环索引

            foreach (var entry in Types)
            {
                if (entry == null || entry.Count <= 0) continue;

                float radius = RingRadius + ringIdx * RingSpacing;
                int skip = (ringIdx == 0) ? 1 : 0;  // 第一环第 1 个位置是 leader，跳过
                int count = entry.Count - skip;

                // 将当前环上的敌人均匀分布在圆周上
                for (int j = 0; j < count; j++)
                {
                    float angle = (j / (float)count) * 360f;
                    float rad = angle * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * radius;

                    _followers.Add(new FollowerInfo
                    {
                        Enemy = allEnemies[absIndex],
                        LocalOffset = offset
                    });
                    absIndex++;
                }

                ringIdx++;
            }
        }

        /// <summary>
        /// 实例化单个敌人
        /// </summary>
        private AbstractEnemy SpawnSingle(EnemyTypeEnum enemyType)
        {
            var prefab = Resources.Load<GameObject>("Prefab/Enemy/" + enemyType);
            if (prefab == null)
            {
                return null;
            }

            var obj = Instantiate(prefab, transform);
            var enemy = obj.GetComponent<AbstractEnemy>();
            if (enemy == null)
            {
                Destroy(obj);
                return null;
            }

            enemy.InitEnemy();
            return enemy;
        }

        /// <summary>
        /// 销毁所有子物体，清除队形
        /// </summary>
        [ContextMenu("清除队形")]
        public void ClearFormation()
        {
            // 销毁当前所有子物体
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            _leader = null;
            _followers.Clear();
        }

        #endregion

        #region ----- SO 保存/加载 -------------------------

        /// <summary>将面板配置保存到引用的 SO 文件</summary>
        [ContextMenu("保存队形到 SO")]
        public void SaveToSO()
        {
            if (_typesSO == null)
            {
                Debug.LogWarning("[FormationController] 请先在 SO 配置字段拖拽一个 SO 引用");
                return;
            }

#if UNITY_EDITOR
            _typesSO.RingRadius = RingRadius;
            _typesSO.RingSpacing = RingSpacing;
            _typesSO.Types = new List<EnemyCount>(Types);
            UnityEditor.EditorUtility.SetDirty(_typesSO);
            Debug.Log("[FormationController] 已保存队形配置到 " + _typesSO.name);
#endif
        }

        /// <summary>从引用的 SO 文件加载配置到面板</summary>
        [ContextMenu("从 SO 加载队形")]
        public void LoadFromSO()
        {
            if (_typesSO == null)
            {
                Debug.LogWarning("[FormationController] 请先在 SO 配置字段拖拽一个 SO 引用");
                return;
            }

            RingRadius = _typesSO.RingRadius;
            RingSpacing = _typesSO.RingSpacing;
            Types = new List<EnemyCount>(_typesSO.Types);
            Debug.Log("[FormationController] 已从 " + _typesSO.name + " 加载队形配置");
        }

        #endregion

        #region ----- Gizmos 调试 -------------------------

        /// <summary>
        /// 在 Scene 视图中绘制队形布局与运行时状态
        /// </summary>
        private void OnDrawGizmos()
        {
            var types = Types;
            if (types == null || types.Count == 0) return;

            Vector3 center = Application.isPlaying && _leader != null
                ? _leader.transform.position
                : transform.position;

            // 标记 leader 位置
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(center, 0.35f);

            // 遍历每个环，绘制圆环和目标位置
            for (int ringIdx = 0; ringIdx < types.Count; ringIdx++)
            {
                var entry = types[ringIdx];
                if (entry == null || entry.Count <= 0) continue;

                float radius = RingRadius + ringIdx * RingSpacing;
                int skip = (ringIdx == 0) ? 1 : 0;
                int count = entry.Count - skip;
                if (count <= 0) continue;

                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                Gizmos.DrawWireSphere(center, radius);

                // 绘制每个位置点和辐射线
                Gizmos.color = Color.cyan;
                for (int j = 0; j < count; j++)
                {
                    float angle = (j / (float)count) * 360f;
                    float rad = angle * Mathf.Deg2Rad;
                    Vector3 pos = center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * radius;

                    Gizmos.DrawLine(center, pos);
                    Gizmos.DrawSphere(pos, 0.25f);
                }
            }

            // 运行时显示 follower 实际位置与偏移偏差
            if (Application.isPlaying)
            {
                foreach (var info in _followers)
                {
                    if (info.Enemy == null || info.Enemy.IsDead()) continue;

                    Vector3 target = _leader.transform.position + info.LocalOffset;
                    Vector3 actual = info.Enemy.transform.position;

                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
                    Gizmos.DrawSphere(actual, 0.2f);

                    // 偏差较大时画红线提示
                    if (Vector3.Distance(actual, target) > 0.1f)
                    {
                        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
                        Gizmos.DrawLine(target, actual);
                    }
                }
            }

        }

        #endregion
    }
}
