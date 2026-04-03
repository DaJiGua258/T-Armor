using UnityEngine;

/// <summary>
/// 挂在 SpriteStacking 子物体（位置标记）上，补偿精灵堆叠的视觉高度偏移。
///
/// 问题根源：SpriteStacking Shader 在几何阶段将每一层沿世界 Y 轴偏移
///   worldPos += StackDir * (i * _YOffset)
/// 该偏移始终在世界空间，不随父物体旋转。当父物体旋转后，子物体跟随父物体
/// 的 Transform 旋转，但精灵上特征点的视觉位置（带有世界 Y 高度偏移）却没有
/// 等价旋转，导致视觉位置与 Transform 位置产生偏差。
///
/// 修正原理：
///   特征点视觉世界坐标 = parent.TransformPoint(baseLocal) + Vector3.up * dy
///   要让 child.position 与之一致，需在局部坐标上补偿：
///     correction(local) = parent.InverseTransformVector(Vector3.up * dy)
///   该方法自动处理父物体的旋转与缩放。
/// </summary>
[ExecuteAlways]
public class StackingChildCorrector : MonoBehaviour
{
    [Tooltip("基础本地坐标：在不考虑堆叠高度时，标记点期望在父物体局部空间中的位置。\n" +
             "可在父物体朝向默认方向时手动摆放好，再点击右键菜单「捕获当前位置为基础坐标」。")]
    public Vector3 baseLocalPosition;

    [Tooltip("目标层序号（0 = 最底层）。脚本会自动从父物体 SpriteStacking 读取层间距来计算实际高度偏移。")]
    public float targetLayerIndex = 0;

    [Tooltip("（可选）手动指定父物体的 SpriteStacking，为空时自动查找。")]
    public SpriteStacking parentStacking;

    // -----------------------------------------------------------------------

    private void OnEnable()
    {
        if (parentStacking == null)
            parentStacking = GetComponentInParent<SpriteStacking>();
    }

    private void LateUpdate()
    {
        ApplyCorrection();
    }

    private void ApplyCorrection()
    {
        if (transform.parent == null) return;

        float dy = GetVisualHeightOffset();
        if (Mathf.Approximately(dy, 0f))
        {
            transform.localPosition = baseLocalPosition;
            return;
        }

        // 将世界空间的层叠 Y 偏移量换算到父物体局部空间（含旋转与缩放）
        Vector3 correction = transform.parent.InverseTransformVector(Vector3.up * dy);
        transform.localPosition = baseLocalPosition + correction;
    }

    private float GetVisualHeightOffset()
    {
        if (parentStacking != null)
            return Mathf.Max(0, targetLayerIndex) * parentStacking.ActiveYOffset;

        return 0f;
    }

    // -----------------------------------------------------------------------

    /// <summary>
    /// 右键菜单辅助：将当前 localPosition 反推为 baseLocalPosition（去除修正量后的基础坐标）。
    /// 使用方式：先在场景中把子物体拖到目标视觉位置，再在 Inspector 右键调用此方法。
    /// </summary>
    [ContextMenu("捕获当前位置为基础坐标 (Capture base from current pose)")]
    public void CaptureBasePosition()
    {
        if (transform.parent == null)
        {
            baseLocalPosition = transform.localPosition;
            return;
        }

        float dy = GetVisualHeightOffset();
        Vector3 correction = transform.parent.InverseTransformVector(Vector3.up * dy);
        baseLocalPosition = transform.localPosition - correction;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // -----------------------------------------------------------------------

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (transform.parent == null) return;

        // 蓝色球：基础坐标（地面层位置）
        Vector3 baseWorld = transform.parent.TransformPoint(baseLocalPosition);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(baseWorld, 0.04f);

        // 黄线：从基础坐标到实际（修正后）位置
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.8f);
        Gizmos.DrawLine(baseWorld, transform.position);
        Gizmos.DrawWireSphere(transform.position, 0.04f);
    }
#endif
}