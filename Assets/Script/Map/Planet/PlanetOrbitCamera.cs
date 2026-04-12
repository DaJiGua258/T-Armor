using UnityEngine;

/// <summary>
/// 摄像机绕星球 Orbit 旋转控制器。
/// 挂在摄像机所在的 GameObject 上。
/// 星球本身 Transform 完全不动；摄像机始终注视 <see cref="planetCenter"/>。
///
/// 使用说明：
///   - 将此脚本挂到摄像机 GameObject
///   - 把星球中心的 Transform 拖入 planetCenter 字段
///   - 调整 orbitRadius / pitchRange 等参数
/// </summary>
public class PlanetOrbitCamera : MonoBehaviour
{
    [Header("目标")]
    [Tooltip("星球中心的 Transform（或任意空对象）")]
    public Transform planetCenter;

    [Header("初始角度（度）")]
    public float initialYaw   = 0f;
    public float initialPitch = 20f;

    [Header("轨道半径")]
    [Tooltip("摄像机与星球中心的距离")]
    public float orbitRadius = 5f;
    [Tooltip("滚轮缩放范围（最小/最大半径）")]
    public Vector2 radiusRange = new Vector2(2f, 12f);
    [Tooltip("滚轮缩放速度")]
    public float zoomSpeed = 1f;

    [Header("拖拽旋转速度")]
    public float dragSpeed = 0.3f;

    [Header("俯仰限制（度）")]
    public Vector2 pitchRange = new Vector2(-80f, 80f);

    // ── 内部状态 ──────────────────────────────────────────────

    private float _yaw;
    private float _pitch;
    private Vector3 _lastMousePos;
    private bool  _dragging;

    private void Start()
    {
        _yaw   = initialYaw;
        _pitch = initialPitch;
        ApplyOrbit();
    }

    private void Update()
    {
        HandleDrag();
        HandleZoom();
    }

    // ── 拖拽旋转 ──────────────────────────────────────────────

    private void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _dragging     = true;
            _lastMousePos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            _dragging = false;
        }

        if (!_dragging) return;

        Vector3 delta = Input.mousePosition - _lastMousePos;
        _lastMousePos = Input.mousePosition;

        _yaw   += delta.x * dragSpeed;
        _pitch -= delta.y * dragSpeed; // 向上拖 → 仰角增加
        _pitch  = Mathf.Clamp(_pitch, pitchRange.x, pitchRange.y);

        ApplyOrbit();
    }

    // ── 滚轮缩放（调整轨道半径）──────────────────────────────

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;

        orbitRadius = Mathf.Clamp(orbitRadius - scroll * zoomSpeed, radiusRange.x, radiusRange.y);
        ApplyOrbit();
    }

    // ── 应用 Orbit 位置 ───────────────────────────────────────

    private void ApplyOrbit()
    {
        if (planetCenter == null) return;

        Quaternion rot      = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3    offset   = rot * Vector3.back * orbitRadius; // back = -Z，绕原点旋转
        transform.position  = planetCenter.position + offset;
        transform.LookAt(planetCenter.position, Vector3.up);
    }

    /// <summary>
    /// 外部调用：将视角平滑对准某个球面方向（可选，后续按需实装）。
    /// </summary>
    public void FocusDirection(Vector3 direction)
    {
        // 从单位向量反算 yaw / pitch
        _yaw   = Mathf.Atan2(direction.x, -direction.z) * Mathf.Rad2Deg;
        _pitch = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) * Mathf.Rad2Deg;
        _pitch = Mathf.Clamp(_pitch, pitchRange.x, pitchRange.y);
        ApplyOrbit();
    }
}
