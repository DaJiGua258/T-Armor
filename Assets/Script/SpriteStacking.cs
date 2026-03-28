using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SpriteStacking : MonoBehaviour
{
    [Header("精灵堆叠设置")]
    // 包含从左到右所有切片的纹理
    public Texture2D SpriteSheet;
    private int LayerCount
    {
        get
        {
            if(_layerCout == 0)
                _layerCout = SpriteSheet.width / 32;
            // Debug.Log(gameObject.name + "layerCount: " + _layerCout);
            return _layerCout;
        }
    }
    private int _layerCout;

    public float YOffset = 0.02f;

    [Header("渲染顺序（Z 轴深度）")]
    public Transform OrderParent;   // 指定排序父物体；设置后排序跟随父物体 Y 轴，而非自身
    public float ZSortOffset = 0f;  // 在基础 Z 值上的额外偏移
    private float _yToZScale = 0.01f;  // Y → Z 的缩放系数（正值 = Y 越大越靠后渲染）

    private MeshFilter _meshFilter;
    private Material _stackingMaterial;
    private Renderer _cachedRenderer;

    void OnEnable()
    {
        Init();
    }

    void Update()
    {
        if (!Application.isPlaying || transform.hasChanged)
        {
            // UpdateMaterial();
            UpdateZSort();
            transform.hasChanged = false;  // 重置，防止自身修改 Z 触发下帧重复计算
        }
    }

    private void OnValidate()
    {
        Init();
        UpdateZSort();
        UpdateMaterial();
    }

    void UpdateMaterial()
    {
        if (_stackingMaterial != null && SpriteSheet != null)
        {
            _stackingMaterial.SetTexture("_MainTex", SpriteSheet);
            _stackingMaterial.SetInt("_LayerCount", LayerCount);
            _stackingMaterial.SetFloat("_YOffset", YOffset);
        }
    }

    /// <summary>
    /// 根据 Y 轴计算目标 Z 值。Y 越大 → Z 越大 → 越靠后渲染（深度缓冲排序）。
    /// </summary>
    public float GetZSort()
    {
        float sourceY;
        if (OrderParent != null)
            sourceY = OrderParent.position.y + transform.localPosition.y;
        else
            sourceY = transform.position.y;

        return sourceY * _yToZScale + ZSortOffset * _yToZScale;
    }

    void UpdateZSort()
    {
        Vector3 pos = transform.position;
        pos.z = GetZSort();
        transform.position = pos;
    }

    private Mesh CreateQuad()
    {
        Mesh mesh = new Mesh { name = "StackingQuad" };
        mesh.vertices = new Vector3[] {
            new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f,  0.5f, 0), new Vector3(0.5f,  0.5f, 0)
        };
        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        mesh.uv = new Vector2[] {
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 1), new Vector2(1, 1)
        };
        return mesh;
    }

    private void Init()
    {
        _meshFilter = GetComponent<MeshFilter>();
        if (_meshFilter.sharedMesh == null)
            _meshFilter.sharedMesh = CreateQuad();

        _cachedRenderer = GetComponent<Renderer>();

        // 只从面板处指定材质
        if (_stackingMaterial == null)
        {
            _stackingMaterial = _cachedRenderer.sharedMaterial;
            if (_stackingMaterial == null)
            {
                Debug.LogError(gameObject.name + "材质为空！");
                return;
            }
        }

        UpdateMaterial();
        UpdateZSort();
    }
}
