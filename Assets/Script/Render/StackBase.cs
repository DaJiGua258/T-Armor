// StackBase.cs
using UnityEngine;

public class StackBase : StackBaseStatic
{
    protected override bool UseRenderManagerStaticMode => true;

    void Update()
    {
        UpdateZSort();
    }
}