using System.Collections.Generic;
using UnityEngine;

public sealed class RenderManager : MonoBehaviour
{
    private static RenderManager _instance;

    public static RenderManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[RenderManager]");
                _instance = go.AddComponent<RenderManager>();
            }

            return _instance;
        }
    }

    private readonly Dictionary<BatchKey, RenderBatch> _batchDict = new Dictionary<BatchKey, RenderBatch>(16);
    private readonly HashSet<Material> _instancingUnsupportedWarned = new HashSet<Material>();
    private Mesh _sharedQuad;

    private readonly struct BatchKey : System.IEquatable<BatchKey>
    {
        public readonly Material Material;
        public readonly MaterialPropertyBlock Properties;

        public BatchKey(Material material, MaterialPropertyBlock properties)
        {
            Material = material;
            Properties = properties;
        }

        public bool Equals(BatchKey other) => Material == other.Material && Properties == other.Properties;

        public override bool Equals(object obj) => obj is BatchKey other && Equals(other);

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (Material != null ? Material.GetHashCode() : 0);
            hash = hash * 23 + (Properties != null ? Properties.GetHashCode() : 0);
            return hash;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        EnsureSharedQuad();
    }

    /// <summary>提交一个实例（不带 per-instance 属性）</summary>
    public void Submit(Material mat, Matrix4x4 matrix)
    {
        Submit(mat, matrix, null);
    }

    /// <summary>提交一个实例（带 per-instance 属性）</summary>
    public void Submit(Material mat, Matrix4x4 matrix, MaterialPropertyBlock properties)
    {
        if (mat == null) return;

        if (!EnsureInstancingReady(mat)) return;

        var key = new BatchKey(mat, properties);
        if (!_batchDict.TryGetValue(key, out var batch))
        {
            EnsureSharedQuad();
            batch = new RenderBatch(_sharedQuad, mat, properties);
            _batchDict.Add(key, batch);
        }

        batch.MatrixList.Add(matrix);
    }

    private bool EnsureInstancingReady(Material mat)
    {
        if (mat.enableInstancing) return true;

        mat.enableInstancing = true;
        if (mat.enableInstancing) return true;

        if (_instancingUnsupportedWarned.Add(mat))
            Debug.LogWarning($"RenderManager: 材质 {mat.name} 未启用/不支持 Instancing，已跳过该材质的实例化绘制。");

        return false;
    }

    private void LateUpdate()
    {
        var enumerator = _batchDict.GetEnumerator();
        while (enumerator.MoveNext())
        {
            RenderBatch batch = enumerator.Current.Value;
            batch.Render();
            batch.Clear();
        }
    }

    private void EnsureSharedQuad()
    {
        if (_sharedQuad != null) return;

        _sharedQuad = new Mesh { name = "RenderManager_Quad" };
        _sharedQuad.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f)
        };
        _sharedQuad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        _sharedQuad.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        _sharedQuad.RecalculateBounds();
    }
}
