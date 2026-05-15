using UnityEngine;
using System.Collections.Generic;
using QFramework.System;

namespace QFramework.ViewController.UI
{
    public class PlanetNodeList : AbstractBasePanel
    {
        [Header("引用")]
        public Transform Planet;            // 星球模型
        public Transform NodeLines;
        public GameObject NodePrefab;       // UI Prefab (Canvas - World Space)
        public GameObject Line;             // 连线预制体（需要 NodeLine + LineRenderer）

        [Header("分布参数")]
        public float PlanetRadius = 1.0f;   // 星球的实际物理半径
        public float SurfaceOffset = 0.2f;  // 关键：让UI离地表的高度，防止切断
        public int NodeCount = 10;
        public int Seed = 42;
        [Range(-1f, 1f)] public float SunlitDotThreshold = 0.0f;
        public int MaxAttemptsMultiplier = 16;
        public Light MainLightOverride;

        [Header("连线参数")]
        public Color FinishedLineColor = new Color(0.23f, 0.72f, 1f, 1f);
        public Color HistoryToNewLineColor = new Color(1f, 0.78f, 0.27f, 1f);


        #region ----- 运行时缓存 ------------------------------
        private readonly List<GameObject> _finishedNodes = new List<GameObject>(); // 已完成历史节点
        private readonly List<GameObject> _newNodes = new List<GameObject>();      // 新生成续选节点
        private readonly List<GameObject> _lineObjects = new List<GameObject>();   // 所有已生成连线
        #endregion

        #region ----- 对外生成 API ------------------------------
        public List<GameObject> GenerateFromLevelOrderAndContinue(
            PlanetGenerator generator,
            IReadOnlyList<LevelDataModel> orderedLevels,
            int continueNodeCount,
            float maxAngularDistanceDeg)
        {
            ClearGeneratedObjects();
            if (!CanGenerateNodes(generator)) return BuildCombinedNodeList();

            if (orderedLevels == null || orderedLevels.Count == 0)
            {
                Debug.LogWarning("PlanetNodeList: 历史关卡为空，回退到随机初始化。");
                return InitNodes(generator);
            }

            bool cacheReady = generator.BuildNoiseCacheSync();
            if (!cacheReady)
            {
                Debug.LogError("PlanetNodeList: 噪声缓存构建失败，无法按顺序生成节点。");
                return BuildCombinedNodeList();
            }

            Vector3 lightDir = ResolveMainLightDirection();
            Vector3? lastNormal = null;

            // 按关卡历史顺序回放节点
            for (int i = 0; i < orderedLevels.Count; i++)
            {
                LevelDataModel levelData = orderedLevels[i];
                if (levelData == null || levelData.EnvironmentData == null)
                {
                    continue;
                }

                EnvironmentData env = levelData.EnvironmentData;
                if (env.SurfaceNormal.sqrMagnitude <= 0.0001f)
                {
                    continue;
                }

                Vector3 surfaceNormal = env.SurfaceNormal.normalized;
                PlanetNodeMapData mapData = generator.EvaluateNodeMapData(surfaceNormal, lightDir, SunlitDotThreshold);

                mapData.Seed = levelData.seed.Value;
                mapData.environmentData.terrainType = env.terrainType;
                mapData.environmentData.moistureType = env.moistureType;
                mapData.environmentData.plantLevelType = env.plantLevelType;
                mapData.environmentData.SurfaceNormal = surfaceNormal;

                SpawnNode(mapData, surfaceNormal, _finishedNodes);

                lastNormal = surfaceNormal;
            }

            // 没有有效历史节点时，回退随机初始化
            if (!lastNormal.HasValue)
            {
                Debug.LogWarning("PlanetNodeList: 历史关卡中没有有效法线数据，回退到随机初始化。");
                return InitNodes(generator);
            }

            // 以最后一个历史节点为中心继续生成可选节点
            int targetNewCount = Mathf.Max(0, continueNodeCount);
            int maxAttempts = Mathf.Max(targetNewCount * Mathf.Max(1, MaxAttemptsMultiplier), targetNewCount);
            int attempts = 0;
            int generated = 0;

            while (generated < targetNewCount && attempts < maxAttempts)
            {
                attempts++;
                Vector3 randomDir = SampleDirectionInCone(lastNormal.Value, maxAngularDistanceDeg);
                PlanetNodeMapData mapData = generator.EvaluateNodeMapData(randomDir, lightDir, SunlitDotThreshold);

                if (!mapData.IsLand || !mapData.IsSunlit)
                {
                    continue;
                }

                SpawnNode(mapData, randomDir, _newNodes);
                generated++;
            }

            if (generated < targetNewCount)
            {
                Debug.LogWarning($"PlanetNodeList: 续生成仅完成 {generated}/{targetNewCount} 个新节点（尝试 {attempts}/{maxAttempts}）。");
            }

            BuildNodeLines();
            return BuildCombinedNodeList();
        }

        public List<GameObject> GenerateFromSelectedNodeContext(
            PlanetGenerator generator,
            EnvironmentData selectedEnv,
            int newNodeCount,
            float maxAngularDistanceDeg)
        {
            ClearGeneratedObjects();
            if (!CanGenerateNodes(generator)) return BuildCombinedNodeList();

            if (selectedEnv == null || selectedEnv.SurfaceNormal.sqrMagnitude <= 0.0001f)
            {
                Debug.LogWarning("PlanetNodeList: 未提供有效的旧节点法线，无法进行续生成。");
                return BuildCombinedNodeList();
            }

            bool cacheReady = generator.BuildNoiseCacheSync();
            if (!cacheReady)
            {
                Debug.LogError("PlanetNodeList: 噪声缓存构建失败，无法进行续生成。");
                return BuildCombinedNodeList();
            }

            Vector3 centerNormal = selectedEnv.SurfaceNormal.normalized;
            Vector3 lightDir = ResolveMainLightDirection();

            // 先恢复上一次选中的旧节点，保证续生成有明确起点。
            PlanetNodeMapData centerData = generator.EvaluateNodeMapData(centerNormal, lightDir, SunlitDotThreshold);
            centerData.environmentData.terrainType = selectedEnv.terrainType;
            centerData.environmentData.moistureType = selectedEnv.moistureType;
            centerData.environmentData.plantLevelType = selectedEnv.plantLevelType;
            centerData.environmentData.SurfaceNormal = centerNormal;
            SpawnNode(centerData, centerNormal, _finishedNodes);

            int targetNewCount = Mathf.Max(0, newNodeCount);
            int maxAttempts = Mathf.Max(targetNewCount * Mathf.Max(1, MaxAttemptsMultiplier), targetNewCount);
            int attempts = 0;
            int generated = 0;

            while (generated < targetNewCount && attempts < maxAttempts)
            {
                attempts++;
                Vector3 randomDir = SampleDirectionInCone(centerNormal, maxAngularDistanceDeg);
                PlanetNodeMapData mapData = generator.EvaluateNodeMapData(randomDir, lightDir, SunlitDotThreshold);

                if (!mapData.IsLand || !mapData.IsSunlit)
                {
                    continue;
                }

                SpawnNode(mapData, randomDir, _newNodes);
                generated++;
            }

            if (generated < targetNewCount)
            {
                Debug.LogWarning($"PlanetNodeList: 续生成仅完成 {generated}/{targetNewCount} 个新节点（尝试 {attempts}/{maxAttempts}）。");
            }

            BuildNodeLines();
            return BuildCombinedNodeList();
        }

        /// <summary>
        /// 初始化节点
        /// </summary>
        public List<GameObject> InitNodes(PlanetGenerator generator)
        {
            ClearGeneratedObjects();
            if (!CanGenerateNodes(generator)) return BuildCombinedNodeList();

            // 初始化随机种子
            Random.InitState(Seed);
            bool cacheReady = generator.BuildNoiseCacheSync();
            if (!cacheReady)
            {
                Debug.LogError("PlanetNodeList: 噪声缓存构建失败，无法进行陆地判定。请先检查 PlanetGenerator 的 GPU 读回日志。");
                return BuildCombinedNodeList();
            }

            // 获取主光源方向
            Vector3 lightDir = ResolveMainLightDirection();
            int targetCount = Mathf.Max(0, NodeCount); // 目标节点数
            int maxAttempts = Mathf.Max(targetCount * Mathf.Max(1, MaxAttemptsMultiplier), targetCount); // 最大尝试次数
            int attempts = 0;  // 尝试次数
            int rejectedByLand = 0; // 陆地淘汰次数
            int rejectedBySunlit = 0; // 阳面淘汰次数
            float sampledHeightMin = float.MaxValue; // 高度采样最小值
            float sampledHeightMax = float.MinValue; // 高度采样最大值

            // 生成节点
            while (_newNodes.Count < targetCount && attempts < maxAttempts)
            {
                attempts++;
                Vector3 randomDir = Random.onUnitSphere;
                PlanetNodeMapData mapData = generator.EvaluateNodeMapData(randomDir, lightDir, SunlitDotThreshold);

                // 记录高度采样范围
                if (mapData.HeightNoise < sampledHeightMin) sampledHeightMin = mapData.HeightNoise;
                if (mapData.HeightNoise > sampledHeightMax) sampledHeightMax = mapData.HeightNoise;

                if (!mapData.IsLand)
                {
                    rejectedByLand++;
                    continue;
                }

                if (!mapData.IsSunlit)
                {
                    rejectedBySunlit++;
                    continue;
                }

                SpawnNode(mapData, randomDir, _newNodes);
            }

            if (_newNodes.Count < targetCount)
            {
                Debug.LogWarning(
                    $"PlanetNodeList: 仅生成 {_newNodes.Count}/{targetCount} 个节点。尝试={attempts}/{maxAttempts}, " +
                    $"陆地淘汰={rejectedByLand}, 阳面淘汰={rejectedBySunlit}, 高度采样范围=[{sampledHeightMin:F3}, {sampledHeightMax:F3}], " +
                    $"SeaLevel={generator.planet.seaLevel:F3}, 阳面阈值={SunlitDotThreshold:F2}, 光照方向={lightDir}");
            }

            return BuildCombinedNodeList();
        }
        #endregion

        #region ----- 生成辅助 ------------------------------
        private bool CanGenerateNodes(PlanetGenerator generator)
        {
            if (Planet == null || NodePrefab == null || generator == null)
            {
                Debug.LogWarning("PlanetNodeList: 缺少必要引用，无法生成节点。");
                return false;
            }

            return true;
        }

        private Vector3 ResolveMainLightDirection()
        {
            Light source = MainLightOverride != null ? MainLightOverride : RenderSettings.sun;
            if (source != null) return -source.transform.forward.normalized;

            Debug.LogWarning("PlanetNodeList: 未找到主光源(mainLightOverride/RenderSettings.sun)，使用 Vector3.up 作为阳面方向。");
            return Vector3.up;
        }

        private void SpawnNode(PlanetNodeMapData mapData, Vector3 surfaceNormal, List<GameObject> targetList)
        {
            Vector3 spawnPos = Planet.position + surfaceNormal.normalized * (PlanetRadius + SurfaceOffset);
            GameObject node = Instantiate(NodePrefab, spawnPos, Quaternion.identity, transform);

            PlanetNodeController controller = node.GetComponent<PlanetNodeController>();
            if (controller == null)
            {
                controller = node.AddComponent<PlanetNodeController>();
            }

            controller.Init(Planet, mapData);
            targetList.Add(node);
        }

        private Vector3 SampleDirectionInCone(Vector3 centerNormal, float maxAngularDistanceDeg)
        {
            float clampedDeg = Mathf.Clamp(maxAngularDistanceDeg, 0f, 180f);
            if (clampedDeg <= 0.001f)
            {
                return centerNormal.normalized;
            }

            float maxAngleRad = clampedDeg * Mathf.Deg2Rad;
            float cosTheta = Mathf.Lerp(Mathf.Cos(maxAngleRad), 1f, Random.value);
            float sinTheta = Mathf.Sqrt(1f - cosTheta * cosTheta);
            float phi = Random.Range(0f, Mathf.PI * 2f);

            Vector3 localDir = new Vector3(
                sinTheta * Mathf.Cos(phi),
                sinTheta * Mathf.Sin(phi),
                cosTheta);

            Quaternion toCenter = Quaternion.FromToRotation(Vector3.forward, centerNormal.normalized);
            return (toCenter * localDir).normalized;
        }

        private List<GameObject> BuildCombinedNodeList()
        {
            List<GameObject> allNodes = new List<GameObject>(_finishedNodes.Count + _newNodes.Count);
            allNodes.AddRange(_finishedNodes);
            allNodes.AddRange(_newNodes);
            return allNodes;
        }
        #endregion

        #region ----- 连线逻辑 ------------------------------
        private void BuildNodeLines()
        {
            BuildFinishedNodeLines();
            BuildLastFinishedToNewNodeLines();
        }

        private void BuildFinishedNodeLines()
        {
            for (int i = 1; i < _finishedNodes.Count; i++)
            {
                SpawnLine(_finishedNodes[i - 1], _finishedNodes[i], FinishedLineColor);
            }
        }

        private void BuildLastFinishedToNewNodeLines()
        {
            if (_finishedNodes.Count == 0 || _newNodes.Count == 0)
            {
                return;
            }

            GameObject lastFinishedNode = _finishedNodes[_finishedNodes.Count - 1];
            for (int i = 0; i < _newNodes.Count; i++)
            {
                SpawnLine(lastFinishedNode, _newNodes[i], HistoryToNewLineColor);
            }
        }

        private void SpawnLine(GameObject fromNode, GameObject toNode, Color lineColor)
        {
            if (Line == null || fromNode == null || toNode == null || Planet == null)
            {
                return;
            }

            GameObject lineObject = Instantiate(Line, NodeLines);
            NodeLine nodeLine = lineObject.GetComponent<NodeLine>();

            nodeLine.Init(fromNode.transform.position, toNode.transform.position, Planet.position);

            LineRenderer lineRenderer = lineObject.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.startColor = lineColor;
                lineRenderer.endColor = lineColor;
                if (lineRenderer.material != null && lineRenderer.material.HasProperty("_Color"))
                {
                    lineRenderer.material.color = lineColor;
                }
            }

            _lineObjects.Add(lineObject);
        }
        #endregion

        #region ----- 清理逻辑 ------------------------------
        /// <summary>
        /// 重置所有节点的高亮状态（返回 LevelSelect 时调用）
        /// </summary>
        public void ResetNodeHighlight()
        {
            for (int i = 0; i < _finishedNodes.Count; i++)
            {
                if (_finishedNodes[i] != null)
                {
                    var controller = _finishedNodes[i].GetComponent<PlanetNodeController>();
                    if (controller != null) controller.SetHighlighted(false);
                }
            }
            for (int i = 0; i < _newNodes.Count; i++)
            {
                if (_newNodes[i] != null)
                {
                    var controller = _newNodes[i].GetComponent<PlanetNodeController>();
                    if (controller != null) controller.SetHighlighted(false);
                }
            }
        }

        private void ClearGeneratedObjects()
        {
            ClearObjectList(_lineObjects);
            ClearObjectList(_finishedNodes);
            ClearObjectList(_newNodes);
        }

        private static void ClearObjectList(List<GameObject> objectList)
        {
            for (int i = 0; i < objectList.Count; i++)
            {
                if (objectList[i] != null)
                {
                    Destroy(objectList[i]);
                }
            }

            objectList.Clear();
        }
        #endregion


        #region ----- Gizmos ------------------------------
        // 在编辑器窗口直接拉动参数就能看到点的分布
        private void OnDrawGizmosSelected()
        {
            if (Planet == null) return;
            Gizmos.color = Color.yellow;
            Random.InitState(Seed);
            for (int i = 0; i < NodeCount; i++)
            {
                Vector3 pos = Planet.position + Random.onUnitSphere * (PlanetRadius + SurfaceOffset);
                Gizmos.DrawWireSphere(pos, 0.15f);
            }
        }
        #endregion
    }
}