using UnityEngine;

public class InstancingTest : MonoBehaviour
{
    [Header("渲染资源")]
    public Material targetMaterial; // 确保该材质勾选了 Enable GPU Instancing
    
    [Header("生成设置")]
    public int instanceCount = 5000; // 测试 5000 个物体
    public float range = 50f;

    // 缓存矩阵数组，避免在 Update 里进行逻辑计算，纯测试渲染性能
    private Matrix4x4[] _matrices;

    void Start()
    {
        _matrices = new Matrix4x4[instanceCount];

        for (int i = 0; i < instanceCount; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-range, range),
                Random.Range(-range, range),
                Random.Range(-range, range)
            );
            Quaternion rot = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), 0);
            Vector3 scale = Vector3.one * Random.Range(0.5f, 2.0f);

            // 预先计算矩阵
            _matrices[i] = Matrix4x4.TRS(pos, rot, scale);
        }

        Debug.Log($"已准备好 {instanceCount} 个物体的渲染矩阵。");

        
        
    }

    void Update()
    {
        // 模拟每帧提交。在实际项目中，这里可能是你各个单位的逻辑更新
        for (int i = 0; i < instanceCount; i++)
        {
            RenderManager.Instance.Submit(targetMaterial, _matrices[i]);
        }
    }
}