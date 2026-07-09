using System.Collections.Generic;
using UnityEngine;

public sealed class RenderBatch
{
    private const int MaxInstancesPerDraw = 1023;

    public Mesh Mesh { get; }
    public Material Material { get; }
    public MaterialPropertyBlock Properties { get; }
    public List<Matrix4x4> MatrixList { get; }

    private readonly Matrix4x4[] _renderBuffer;

    public RenderBatch(Mesh mesh, Material material, MaterialPropertyBlock properties = null)
    {
        Mesh = mesh;
        Material = material;
        Properties = properties;
        MatrixList = new List<Matrix4x4>(1024);
        _renderBuffer = new Matrix4x4[MaxInstancesPerDraw];
    }

    /// <summary>
    /// 渲染方法，用于执行网格实例化渲染
    /// </summary>
    public void Render()
    {
        // 获取矩阵列表的总数量
        int total = MatrixList.Count;
        // 检查是否有可渲染的内容，如果没有矩阵、网格或材质，则直接返回
        if (total == 0 || Mesh == null || Material == null)
            return;

        int start = 0;
        // 循环处理所有矩阵实例，每次处理一批直到全部处理完毕
        while (start < total)
        {
            // 计算当前批次可以渲染的实例数量
            int count = total - start;
            // 如果剩余实例数超过单次最大渲染数量，则限制为最大数量
            if (count > MaxInstancesPerDraw)
                count = MaxInstancesPerDraw;

            // 将当前批次的矩阵数据复制到渲染缓冲区
            for (int i = 0; i < count; i++)
                _renderBuffer[i] = MatrixList[start + i];

            // 执行实例化渲染调用
            Graphics.DrawMeshInstanced(Mesh, 0, Material, _renderBuffer, count, Properties);
            // 移动到下一批次的起始位置
            start += count;
        }
    }

    public void Clear()
    {
        MatrixList.Clear();
    }
}
