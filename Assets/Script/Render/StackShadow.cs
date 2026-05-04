// StackShadow.cs
using UnityEngine;

public class StackShadow : StackShadowStatic
{
    [Header("位置锁定")]
    public bool IsLock = true;    // true = 每帧锁定到目标位置，false = 只在初始设一次

    protected override bool UseRenderManagerStaticMode => false;

    void Update()
    {
        if (!Application.isPlaying || transform.hasChanged)
        {
            UpdateZSort();
            transform.hasChanged = false;
        }

        if (IsLock)
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
}