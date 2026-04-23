// StackShadow.cs
using UnityEngine;

public class StackShadow : StackingCore
{   
    [Header("缩放参数")]
    public bool  CanSetScale;
    public float SizeMultiplier = 1f;

    [Header("影子设置")]
    public Vector2 ShadowOffset2D = new Vector2(0.1f, 0.1f);

    // 影子在同层 base 后面的偏移量
    private const float ShadowZOffset = 0.01f;

    protected override void OnInit()   
    {
        SyncPosition();
        UpdateScale();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        SyncPosition();
        UpdateScale();
    }

    protected override void Update()
    {
        SyncPosition();
    }

    private void SyncPosition()
    {
        var hierarchyPos = GetHierarchyReferencePosition();

        var pos = transform.position;
        pos.x = hierarchyPos.x + ShadowOffset2D.x;
        pos.y = hierarchyPos.y + ShadowOffset2D.y;
        pos.z = GetZSort(HeightLevelZOffset, ShadowZOffset, hierarchyPos.y);
        transform.position = pos;
    }

    protected override void UpdateMaterial()
    {
        base.UpdateMaterial();
        if (StackingMaterial == null) return;
        StackingMaterial.SetInt  ("_LayerCount", 1);
        StackingMaterial.SetFloat("_YOffset",    0f);
    }

    protected void UpdateScale()
    {
        if (!CanSetScale) return;
        transform.localScale = new Vector3(SizeMultiplier, SizeMultiplier, 1f);
    }

    protected override void UpdateZSort() { } // 由 SyncPosition 接管
}