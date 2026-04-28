using UnityEngine;

public class BatchSpawner : MonoBehaviour
{
    [Header("生成设置")]
    public GameObject prefabToSpawn; // 拖入你想生成的预制体（比如你的机甲模型或检测框）
    public int spawnCount = 500;    // 生成数量
    public Vector3 spawnRange = new Vector3(50, 10, 50); // 随机范围

    [Header("可选设置")]
    public bool useRandomRotation = true;
    public Transform parentFolder; // 生成后的父物体，方便清理层级面板

    void Start()
    {
        SpawnObjects();
    }

    [ContextMenu("立即生成测试")] // 可以在 Inspector 面板右键点击脚本手动触发
    public void SpawnObjects()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogError("请先在 Inspector 中分配 PrefabToSpawn！");
            return;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            // 计算随机位置
            Vector3 randomPos = new Vector3(
                Random.Range(-spawnRange.x, spawnRange.x),
                Random.Range(0, spawnRange.y),
                Random.Range(-spawnRange.z, spawnRange.z)
            ) + transform.position;

            // 计算随机旋转
            Quaternion randomRot = useRandomRotation ? 
                Quaternion.Euler(0, Random.Range(0, 360f), 0) : 
                Quaternion.identity;

            // 实例化
            GameObject newObj = Instantiate(prefabToSpawn, randomPos, randomRot);

            // 设置父物体，保持 Hierarchy 整洁
            if (parentFolder != null)
                newObj.transform.SetParent(parentFolder);
        }

        Debug.Log($"成功生成了 {spawnCount} 个物体。");
    }
}