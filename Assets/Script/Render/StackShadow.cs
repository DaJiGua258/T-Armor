// StackShadow.cs
using UnityEngine;

public class StackShadow : StackShadowStatic
{   
    void Update()
    {
        if (!Application.isPlaying || transform.hasChanged)
        {
            UpdateZSort();
            transform.hasChanged = false;
        }
        
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