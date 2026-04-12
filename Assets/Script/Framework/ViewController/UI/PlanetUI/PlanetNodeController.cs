using UnityEngine;

public class PlanetNodeController : MonoBehaviour
{
    private Transform planet;
    private Transform mainCam;

    [Header("缩放设置")]
    public float baseScale = 0.01f;     // World Space UI 基础大小
    public float minScaleLimit = 0.4f;  // 在边缘时的最小比例
    public float maxScaleLimit = 1.0f;  // 正对时的最大比例

    public void Init(Transform planetTransform)
    {
        planet = planetTransform;
        mainCam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (planet == null || mainCam == null) return;

        // 1. 广告牌：始终面向摄像机
        // 使用 LookRotation 让 UI 面板正对相机
        Vector3 dirToCam = mainCam.position - transform.position;
        if (dirToCam != Vector3.zero)
        {
            transform.LookAt(mainCam, mainCam.up);
            transform.Rotate(0, 180, 0);
        }

        // 2. 边缘缩放逻辑：增加空间深度的视觉反馈
        Vector3 nodeNormal = (transform.position - planet.position).normalized;
        // 计算点积：1 代表 UI 在星球正中心对着你，0 代表 UI 在星球边缘
        float dot = Mathf.Clamp01(Vector3.Dot(nodeNormal, dirToCam.normalized));
        
        float scale = Mathf.Lerp(minScaleLimit, maxScaleLimit, dot) * baseScale;
        transform.localScale = new Vector3(scale, scale, scale);

    }
}