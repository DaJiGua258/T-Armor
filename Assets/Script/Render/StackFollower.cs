using UnityEngine;

/// <summary>
/// 挂载到 SpriteStacking 父物体下的子物体上，
/// 模拟 shader 堆叠中的额外一层，始终在世界空间 Y 轴方向保持固定偏移。
/// </summary>
[ExecuteAlways]
public class StackFollower : MonoBehaviour
{
    public float offsetY = 1f;

    // 子物体相对父物体的基础局部坐标（不含 offset）
    public Vector2 _baseLocalPosition;

    void Start()
    {
        // _baseLocalPosition = transform.localPosition;
    }

    void LateUpdate()
    {
        if (transform.parent == null) return;

        // 父物体旋转/移动后，子物体"应在"的世界坐标
        Vector2 baseWorld = transform.parent.TransformPoint(_baseLocalPosition);

        Vector2 res = baseWorld + Vector2.up * offsetY;

        // 再叠加世界空间 Y 轴偏移
        transform.position = new Vector3
        (
            res.x,
            res.y,
            transform.position.z
        );
    }
}
