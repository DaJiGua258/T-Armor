using UnityEngine;

[ExecuteAlways]
public class SimpleYSort : MonoBehaviour
{
    [Header("渲染顺序")]
    public bool UseLocalZSort = true;
    public Transform OrderParent;
    public float ZSortOffset = 0f;

    private const float YToZScale = 0.1f;

    private void OnEnable()
    {
        UpdateZSort();
    }

    private void Start()
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

    private void OnValidate()
    {
        UpdateZSort();
    }

    private void UpdateZSort()
    {
        var pos = transform.position;
        pos.z = GetZSort();
        transform.position = pos;
    }

    /// <param name="heightLevelOffset">高度层基础 Z 偏移，由派生类传入</param>
    /// <param name="shadowOffset">影子额外后推偏移</param>
    public float GetZSort(float heightLevelOffset = 0f, float shadowOffset = 0f)
    {
        float sourceY;
        if (OrderParent != null)
            sourceY = UseLocalZSort ? OrderParent.position.y + transform.localPosition.y : OrderParent.position.y;
        else
            sourceY = transform.position.y;

        return heightLevelOffset + (sourceY + ZSortOffset) * YToZScale + shadowOffset;
    }
}
