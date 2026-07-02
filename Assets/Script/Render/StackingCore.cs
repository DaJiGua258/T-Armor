// StackingCore.cs
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum HeightLevel { Ground = 0, LowAir = 1, HighAir = 2 }

[ExecuteAlways]
public abstract class StackingCore : MonoBehaviour
{
    [Header("堆叠材质")]
    public Material StackingMaterial;

    [Header("渲染顺序")]
    public bool UseLocalZSort = true;
    public Transform OrderParent;
    public float ZSortOffset = 0f;

    [Header("高度层级")]
    public HeightLevel HeightLevel = global::HeightLevel.Ground;

    protected const float YToZScale = 0.1f;

    // 高度层 Z 基础偏移，越小越靠前渲染
    protected const float ZOffsetGround  =   0f;
    protected const float ZOffsetLowAir  = -5f;
    protected const float ZOffsetHighAir = -10f;

    protected MeshFilter   MeshFilter;
    protected MeshRenderer CachedRenderer;

    protected virtual bool UseRenderManagerStaticMode => false;

    public float HeightLevelZOffset => HeightLevel switch
    {
        global::HeightLevel.Ground  => ZOffsetGround,
        global::HeightLevel.LowAir  => ZOffsetLowAir,
        global::HeightLevel.HighAir => ZOffsetHighAir,
        _                   => ZOffsetGround,
    };

    protected virtual void OnEnable()  => Init();
    protected virtual void Start()     => Init();

    protected virtual void OnValidate()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
            return;

        EditorApplication.delayCall += () =>
        {
            if (this == null || Application.isPlaying)
                return;

            Init();
        };
#endif
    }

    // ── per-instance 自定义属性块（用于受击闪白等效果）──
    protected MaterialPropertyBlock _customPropertyBlock;

    public void SetCustomPropertyBlock(MaterialPropertyBlock block)
    {
        _customPropertyBlock = block;
    }

    public void ClearCustomPropertyBlock()
    {
        _customPropertyBlock = null;
    }

    protected virtual void Init()
    {
        if (Application.isPlaying && UseRenderManagerStaticMode)
        {
            RemoveMeshComponents();
            UpdateMaterial();
            OnInit();
            return;
        }

        if (!EnsureMeshComponents())
            return;

        UpdateMaterial();
        OnInit();
    }

    protected virtual void OnInit() { }

    protected virtual void UpdateMaterial()
    {
        if (CachedRenderer == null) return;
        CachedRenderer.sharedMaterial = StackingMaterial;
    }

    protected Mesh CreateQuad()
    {
        var mesh = new Mesh { name = "StackingQuad" };
        mesh.vertices  = new[] { new Vector3(-0.5f,-0.5f,0), new Vector3(0.5f,-0.5f,0), new Vector3(-0.5f,0.5f,0), new Vector3(0.5f,0.5f,0) };
        mesh.triangles = new[] { 0,2,1,2,3,1 };
        mesh.uv        = new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(0,1), new Vector2(1,1) };
        return mesh;
    }

    protected Vector3 GetHierarchyReferencePosition()
    {
        return transform.parent != null ? transform.parent.position : transform.position;
    }

    protected float ResolveSortSourceY(float? sourceYOverride = null)
    {
        if (sourceYOverride.HasValue)
            return sourceYOverride.Value;

        if (OrderParent != null)
            return UseLocalZSort ? OrderParent.position.y + transform.localPosition.y : OrderParent.position.y;

        return transform.position.y;
    }

    /// <param name="heightLevelOffset">高度层基础 Z 偏移，由派生类传入</param>
    /// <param name="shadowOffset">影子额外后推偏移</param>
    /// <param name="sourceYOverride">可选排序来源 Y（用于共享层级计算）</param>
    public float GetZSort(float heightLevelOffset = 0f, float shadowOffset = 0f, float? sourceYOverride = null)
    {
        float sourceY = ResolveSortSourceY(sourceYOverride);
        return heightLevelOffset + (sourceY + ZSortOffset) * YToZScale + shadowOffset;
    }

    protected virtual void UpdateZSort() { }

    private bool EnsureMeshComponents()
    {
        if (StackingMaterial == null)
        {
            RemoveMeshComponents();
            return false;
        }

        MeshFilter = GetComponent<MeshFilter>();
        if (MeshFilter == null)
            MeshFilter = gameObject.AddComponent<MeshFilter>();

        CachedRenderer = GetComponent<MeshRenderer>();
        if (CachedRenderer == null)
            CachedRenderer = gameObject.AddComponent<MeshRenderer>();

        if (MeshFilter.sharedMesh == null)
            MeshFilter.sharedMesh = CreateQuad();

        CachedRenderer.enabled = true;
        return true;
    }

    private void RemoveMeshComponents()
    {
        if (!gameObject.scene.IsValid())
        {
            MeshFilter = null;
            CachedRenderer = null;
            return;
        }

        var meshFilter = GetComponent<MeshFilter>();
        var meshRenderer = GetComponent<MeshRenderer>();

        if (Application.isPlaying)
        {
            if (meshFilter != null) Destroy(meshFilter);
            if (meshRenderer != null) Destroy(meshRenderer);
        }
        else
        {
            if (meshFilter != null) DestroyImmediate(meshFilter);
            if (meshRenderer != null) DestroyImmediate(meshRenderer);
        }

        MeshFilter = null;
        CachedRenderer = null;
    }
}