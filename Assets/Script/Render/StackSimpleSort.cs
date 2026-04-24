using UnityEngine;

public class StackSimpleSort : StackingCore
{
    protected override void OnEnable()
    {
        UpdateZSort();
    }

    protected override void Start()
    {
        UpdateZSort();
    }

    private void Update()
    {
        if (!Application.isPlaying || transform.hasChanged)
        {
            UpdateZSort();
            transform.hasChanged = false;
        }
    }

    protected override void OnValidate()
    {
        UpdateZSort();
    }

    protected override void UpdateZSort()
    {
        var pos = transform.position;
        pos.z = GetZSort();
        transform.position = pos;
    }
}
