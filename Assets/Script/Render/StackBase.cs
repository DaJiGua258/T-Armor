// StackBase.cs
using UnityEngine;

public enum HeightLevel { Ground = 0, LowAir = 1, HighAir = 2 }

public class StackBase : StackingCore
{
    [Header("缩放参数")]
    public bool  CanSetScale;
    public float SizeMultiplier = 1f;
    public float SizeHeight     = 1f;

    [Header("高度层级")]
    public HeightLevel HeightLevel = HeightLevel.Ground;

    [Header("高度")]
    [Min(0f)]
    public float AirHeight = 0f;

    private float _prevAirHeight = 0f;

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

    // 根据 HeightLevel 返回对应 Z 基础偏移
    public float HeightLevelZOffset => HeightLevel switch
    {
        HeightLevel.Ground  => ZOffsetGround,
        HeightLevel.LowAir  => ZOffsetLowAir,
        HeightLevel.HighAir => ZOffsetHighAir,
        _                   => ZOffsetGround,
    };

    protected override void OnInit()
    {
        _prevAirHeight = AirHeight;
        UpdateScale();
        UpdateZSort();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        UpdateScale();
        ApplyAirHeightToParent();
    }

    protected override void Update()
    {
        base.Update();
        if (!Mathf.Approximately(AirHeight, _prevAirHeight))
            ApplyAirHeightToParent();
    }

    protected override void UpdateZSort()
    {
        var pos = transform.position;
        pos.z = GetZSort(HeightLevelZOffset);
        transform.position = pos;
    }

    private void ApplyAirHeightToParent()
    {
        var parent = transform.parent;
        if (parent == null) return;

        float delta = AirHeight - _prevAirHeight;
        var pos = parent.position;
        pos.y += delta;
        parent.position = pos;
        _prevAirHeight = AirHeight;
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