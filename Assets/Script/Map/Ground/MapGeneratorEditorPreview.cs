using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class MapGenerator
{
#if UNITY_EDITOR
    private bool _previewPending;

    private void OnValidate()
    {
        EnsureSettingsObjects();
        if (_previewPending) return;
        _previewPending = true;

        EditorApplication.delayCall += () =>
        {
            _previewPending = false;
            if (this == null) return;
            RebuildPreview();
        };
    }

    private void RebuildPreview()
    {
        if (terrain.layers == null || terrain.layers.Length == 0 || mapSize < 1) return;

        BuildGrid();
        _gizmoObstacles = ComputeObstaclePlacements();
        SceneView.RepaintAll();
    }

    private void TryApplyParameterAssetFromInspector()
    {
        if (_isApplyingParameterAsset || parameterAsset == null) return;
        if (parameterAsset == _appliedParameterAsset) return;
        ApplyParametersFromAsset(parameterAsset);
    }
#endif
}
