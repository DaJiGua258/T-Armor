// StackBase.cs
using UnityEngine;

public class StackBase : StackBaseStatic
{
    protected override bool UseRenderManagerStaticMode => false;


    void Update()
    {
        UpdateZSort();   
        if (!Application.isPlaying || transform.hasChanged)
        {
            
            transform.hasChanged = false;
        }
    }
}