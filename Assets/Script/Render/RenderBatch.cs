using System.Collections.Generic;
using UnityEngine;

public sealed class RenderBatch
{
    private const int MaxInstancesPerDraw = 1023;

    public Mesh Mesh { get; }
    public Material Material { get; }
    public List<Matrix4x4> MatrixList { get; }

    private readonly Matrix4x4[] _renderBuffer;

    public RenderBatch(Mesh mesh, Material material)
    {
        Mesh = mesh;
        Material = material;
        MatrixList = new List<Matrix4x4>(1024);
        _renderBuffer = new Matrix4x4[MaxInstancesPerDraw];
    }

    public void Render()
    {
        int total = MatrixList.Count;
        if (total == 0 || Mesh == null || Material == null)
        {
            return;
        }

        int start = 0;
        while (start < total)
        {
            int count = total - start;
            if (count > MaxInstancesPerDraw)
            {
                count = MaxInstancesPerDraw;
            }

            for (int i = 0; i < count; i++)
            {
                _renderBuffer[i] = MatrixList[start + i];
            }

            Graphics.DrawMeshInstanced(Mesh, 0, Material, _renderBuffer, count);
            start += count;
        }
    }

    public void Clear()
    {
        MatrixList.Clear();
    }
}
