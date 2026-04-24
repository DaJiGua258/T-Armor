// StackBase.cs
using UnityEngine;

public class StackBase : StackBaseStatic
{
    void Update()
    {
        if (!Application.isPlaying || transform.hasChanged)
        {
            UpdateZSort();
            transform.hasChanged = false;
        }
    }
}