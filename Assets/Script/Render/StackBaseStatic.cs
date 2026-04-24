// StackBaseStatic.cs
using UnityEngine;

public class StackBaseStatic : StackingCore
{
    [Header("缩放参数")]
    public bool  CanSetScale;
    public float SizeMultiplier = 1f;
    public float SizeHeight     = 1f;

    private const float BaseYOffset = 0.03f;
    private float _yOffset = BaseYOffset;
    public float ActiveYOffset => _yOffset;

    private int _layerCount;
    private int LayerCount
    {
        get
        {
            if (_layerCount == 0 && SpriteSheet != null)
                _layerCount = SpriteSheet.width / SpriteSize;
            return _layerCount;
        }
    }

    protected override void OnInit()
    {
        UpdateScale();
        UpdateZSort();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        UpdateScale();
    }

    protected override void UpdateZSort()
    {
        var pos = transform.position;
        pos.z = GetZSort(HeightLevelZOffset);
        transform.position = pos;
    }

    protected override void UpdateMaterial()
    {
        base.UpdateMaterial();
        if (StackingMaterial == null) return;
        StackingMaterial.SetInt  ("_LayerCount", LayerCount);
        StackingMaterial.SetFloat("_YOffset",    _yOffset);
    }

    protected void UpdateScale()
    {
        _yOffset = BaseYOffset;
        if (!CanSetScale) return;
        transform.localScale = new Vector3(SizeMultiplier, SizeMultiplier, 1f);
        _yOffset = SizeHeight * 0.02f;
    }
}
