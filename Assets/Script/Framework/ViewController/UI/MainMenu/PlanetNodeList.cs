using UnityEngine;
using System.Collections.Generic;
using QFramework.Event;

namespace QFramework.ViewController.UI
{
    public class PlanetNodeList : AbstractBasePanel
    {
        [Header("引用")]
        public Transform planet;            // 星球模型
        public GameObject nodePrefab;       // UI Prefab (Canvas - World Space)

        [Header("分布参数")]
        public float planetRadius = 5.0f;   // 星球的实际物理半径
        public float surfaceOffset = 0.2f;  // 关键：让UI离地表的高度，防止切断
        public int nodeCount = 10;
        public int seed = 42;
        [Range(-1f, 1f)] public float sunlitDotThreshold = 0.0f;
        public int maxAttemptsMultiplier = 16;
        public Light mainLightOverride;

        private List<GameObject> spawnedNodes = new List<GameObject>();

        /// <summary>
        /// 初始化节点
        /// </summary>
        public List<GameObject> InitNodes(PlanetGenerator generator)
        {
            // 清空已生成的节点
            foreach (var n in spawnedNodes) if (n != null) Destroy(n);
            spawnedNodes.Clear();

            // 检查必要引用
            if (planet == null || nodePrefab == null || generator == null)
            {
                Debug.LogWarning("PlanetNodeList: 缺少必要引用，无法生成节点。");
                return spawnedNodes;
            }

            // 初始化随机种子
            Random.InitState(seed);
            bool cacheReady = generator.BuildNoiseCacheSync();
            if (!cacheReady)
            {
                Debug.LogError("PlanetNodeList: 噪声缓存构建失败，无法进行陆地判定。请先检查 PlanetGenerator 的 GPU 读回日志。");
                return spawnedNodes;
            }

            // 获取主光源方向
            Vector3 lightDir = ResolveMainLightDirection();
            int targetCount = Mathf.Max(0, nodeCount);
            int maxAttempts = Mathf.Max(targetCount * Mathf.Max(1, maxAttemptsMultiplier), targetCount);
            int attempts = 0;
            int rejectedByLand = 0;
            int rejectedBySunlit = 0;
            float sampledHeightMin = float.MaxValue;
            float sampledHeightMax = float.MinValue;

            // 生成节点
            while (spawnedNodes.Count < targetCount && attempts < maxAttempts)
            {
                attempts++;
                Vector3 randomDir = Random.onUnitSphere;
                PlanetNodeMapData mapData = generator.EvaluateNodeMapData(randomDir, lightDir, sunlitDotThreshold);

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

                Vector3 spawnPos = planet.position + randomDir * (planetRadius + surfaceOffset);
                GameObject node = Instantiate(nodePrefab, spawnPos, Quaternion.identity, transform);

                PlanetNodeController controller = node.GetComponent<PlanetNodeController>();
                if (controller == null) 
                {
                    controller = node.AddComponent<PlanetNodeController>();
                }
                
                controller.Init(planet, mapData);

                spawnedNodes.Add(node);
            }

            if (spawnedNodes.Count < targetCount)
            {
                Debug.LogWarning(
                    $"PlanetNodeList: 仅生成 {spawnedNodes.Count}/{targetCount} 个节点。尝试={attempts}/{maxAttempts}, " +
                    $"陆地淘汰={rejectedByLand}, 阳面淘汰={rejectedBySunlit}, 高度采样范围=[{sampledHeightMin:F3}, {sampledHeightMax:F3}], " +
                    $"SeaLevel={generator.planet.seaLevel:F3}, 阳面阈值={sunlitDotThreshold:F2}, 光照方向={lightDir}");
            }

            return spawnedNodes;
        }

        private Vector3 ResolveMainLightDirection()
        {
            Light source = mainLightOverride != null ? mainLightOverride : RenderSettings.sun;
            if (source != null) return -source.transform.forward.normalized;

            Debug.LogWarning("PlanetNodeList: 未找到主光源(mainLightOverride/RenderSettings.sun)，使用 Vector3.up 作为阳面方向。");
            return Vector3.up;
        }


        // 在编辑器窗口直接拉动参数就能看到点的分布
        private void OnDrawGizmosSelected()
        {
            if (planet == null) return;
            Gizmos.color = Color.yellow;
            Random.InitState(seed);
            for (int i = 0; i < nodeCount; i++)
            {
                Vector3 pos = planet.position + Random.onUnitSphere * (planetRadius + surfaceOffset);
                Gizmos.DrawWireSphere(pos, 0.15f);
            }
        }
    }
}