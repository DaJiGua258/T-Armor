using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteZSort : MonoBehaviour
{
    [Header("排序设置")]
    public Transform OrderParent;   // 指定排序父物体；设置后排序跟随父物体 Y 轴
    public float ZSortOffset = 0f;  // 在基础 Z 值上的额外偏移

    private readonly float _yToZScale = 0.01f;

    void OnEnable()
    {
        UpdateZSort();
    }

    void Update()
    {
        if (!Application.isPlaying || transform.hasChanged)
        {
            UpdateZSort();
            transform.hasChanged = false;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            UpdateZSort();
        };
    }
#endif

    public float GetZSort()
    {
        float sourceY = OrderParent != null
            ? OrderParent.position.y + transform.localPosition.y
            : transform.position.y;

        return sourceY * _yToZScale + ZSortOffset * _yToZScale;
    }

    void UpdateZSort()
    {
        Vector3 pos = transform.position;
        pos.z = GetZSort();
        transform.position = pos;
    }
}