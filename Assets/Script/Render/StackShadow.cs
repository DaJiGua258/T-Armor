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
        var pos = transform.position;
        pos.z = GetZSort(HeightLevelZOffset, ShadowZOffset, pos.y);
        transform.position = pos;
    }
}