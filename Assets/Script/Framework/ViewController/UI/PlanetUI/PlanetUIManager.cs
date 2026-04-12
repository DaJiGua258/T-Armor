using UnityEngine;
using System.Collections.Generic;

public class PlanetUIManager : MonoBehaviour
{
    [Header("引用")]
    public Transform planet;            // 星球模型
    public GameObject nodePrefab;       // UI Prefab (Canvas - World Space)

    [Header("分布参数")]
    public float planetRadius = 5.0f;   // 星球的实际物理半径
    public float surfaceOffset = 0.2f;  // 关键：让UI离地表的高度，防止切断
    public int nodeCount = 10;
    public int seed = 42;

    private List<GameObject> spawnedNodes = new List<GameObject>();

    void Start()
    {
        if (planet == null || nodePrefab == null) return;
        SpawnNodes();
    }

    public void SpawnNodes()
    {
        foreach (var n in spawnedNodes) if (n != null) Destroy(n);
        spawnedNodes.Clear();

        Random.InitState(seed);

        for (int i = 0; i < nodeCount; i++)
        {
            // 1. 获取球面随机方向
            Vector3 randomDir = Random.onUnitSphere;
            
            // 2. 计算位置：半径 + 偏移量（确保UI完全在球体外）
            Vector3 spawnPos = planet.position + randomDir * (planetRadius + surfaceOffset);

            // 3. 生成
            GameObject node = Instantiate(nodePrefab, spawnPos, Quaternion.identity, transform);
            
            // 4. 初始化控制器
            PlanetNodeController controller = node.GetComponent<PlanetNodeController>();
            if (controller == null) controller = node.AddComponent<PlanetNodeController>();
            
            controller.Init(planet);

            spawnedNodes.Add(node);
        }
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