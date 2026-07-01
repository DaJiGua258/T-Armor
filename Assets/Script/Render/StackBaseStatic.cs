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
    private bool _runtimeInitialized;
    private Matrix4x4 _cachedMatrix;

    protected override bool UseRenderManagerStaticMode => true;

    protected override void OnInit()
    {
        if (Application.isPlaying && UseRenderManagerStaticMode && _runtimeInitialized)
            return;

        UpdateScale();
        UpdateZSort();

        if (Application.isPlaying && UseRenderManagerStaticMode)
        {
            _cachedMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
            _runtimeInitialized = true;
        }
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
        StackingMaterial.SetFloat("_YOffset", _yOffset);
    }

    protected void UpdateScale()
    {
        _yOffset = BaseYOffset;
        if (!CanSetScale) return;
        transform.localScale = new Vector3(SizeMultiplier, SizeMultiplier, 1f);
        _yOffset = SizeHeight * 0.02f;
    }

    private void LateUpdate()
    {
        if (!UseRenderManagerStaticMode || !Application.isPlaying || !_runtimeInitialized || StackingMaterial == null)
            return;

        if (transform.hasChanged)
        {
            _cachedMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
            transform.hasChanged = false;
        }

        RenderManager.Instance.Submit(StackingMaterial, _cachedMatrix);
    }
}