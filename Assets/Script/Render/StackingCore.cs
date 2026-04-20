// StackingCore.cs
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public abstract class StackingCore : MonoBehaviour
{
    [Header("精灵堆叠设置")]
    public Texture2D SpriteSheet;
    public int SpriteSize = 32;

    [Header("渲染顺序")]
    public bool UseLocalZSort = true;
    public Transform OrderParent;
    public float ZSortOffset = 0f;

    protected const float YToZScale = 0.1f;

    // 高度层 Z 基础偏移，越小越靠前渲染
    protected const float ZOffsetGround  =   0f;
    protected const float ZOffsetLowAir  = -8f;
    protected const float ZOffsetHighAir = -16f;

    protected MeshFilter  MeshFilter;
    protected Renderer    CachedRenderer;
    protected Material    StackingMaterial;

    protected virtual void OnEnable()  => Init();
    protected virtual void Start()     => Init();

    protected virtual void Update()
    {
        if (!Application.isPlaying || transform.hasChanged)
        {
            UpdateZSort();
            transform.hasChanged = false;
        }
    }

    protected virtual void OnValidate()
    {
#if UNITY_EDITOR
        EditorApplication.delayCall += () => { if (this != null) Init(); };
#endif
        UpdateZSort();
        UpdateMaterial();
    }

    protected virtual void Init()
    {
        MeshFilter = GetComponent<MeshFilter>();
        if (MeshFilter.sharedMesh == null)
            MeshFilter.sharedMesh = CreateQuad();

        CachedRenderer = GetComponent<Renderer>();

        if (StackingMaterial == null)
        {
            StackingMaterial = CachedRenderer.sharedMaterial;
            if (StackingMaterial == null) { Debug.LogError($"[{gameObject.name}] 材质为空！"); return; }
        }

        UpdateMaterial();
        UpdateZSort();
        OnInit();
    }

    protected virtual void OnInit() { }

    protected virtual void UpdateMaterial()
    {
        if (StackingMaterial == null || SpriteSheet == null) return;
        StackingMaterial.SetTexture("_MainTex", SpriteSheet);
    }

    protected Mesh CreateQuad()
    {
        var mesh = new Mesh { name = "StackingQuad" };
        mesh.vertices  = new[] { new Vector3(-0.5f,-0.5f,0), new Vector3(0.5f,-0.5f,0), new Vector3(-0.5f,0.5f,0), new Vector3(0.5f,0.5f,0) };
        mesh.triangles = new[] { 0,2,1,2,3,1 };
        mesh.uv        = new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(0,1), new Vector2(1,1) };
        return mesh;
    }

    /// <param name="heightLevelOffset">高度层基础 Z 偏移，由派生类传入</param>
    /// <param name="shadowOffset">影子额外后推偏移</param>
    public float GetZSort(float heightLevelOffset = 0f, float shadowOffset = 0f)
    {
        float sourceY;
        if (OrderParent != null)
            sourceY = UseLocalZSort ? OrderParent.position.y + transform.localPosition.y : OrderParent.position.y;
        else
            sourceY = transform.position.y;

        return heightLevelOffset + (sourceY + ZSortOffset) * YToZScale + shadowOffset;
    }

    protected virtual void UpdateZSort() { }
}