using System;
using UnityEngine;

/// <summary>
/// 测试用交互点，挂到场景中即可按 F 测试
/// </summary>
public class TestInteractable : Interactable
{
    private int _interactCount;

    protected override void OnInit()
    {
        _interactCount = 0;
    }

    public override void OnInteract(GameObject player)
    {
        _interactCount++;
        Debug.Log($"[TestInteractable] 第 {_interactCount} 次被互动");
        base.OnInteract(player);
    }

    public override void OnPlaceObject()
    {
        Debug.Log("[TestInteractable] 有物体放置到当前交互点");
        base.OnPlaceObject();
    }
}
