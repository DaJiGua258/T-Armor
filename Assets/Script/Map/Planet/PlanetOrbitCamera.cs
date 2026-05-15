using DG.Tweening;
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
    public bool IsLock = true;
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

    [Header("回退距离")]
    [Tooltip("从节点聚焦状态沿法线回退的目标距离")]
    public float focusReturnRadius = 5f;

    [Header("拖拽旋转速度")]
    public float dragSpeed = 0.3f;

    [Header("俯仰限制（度）")]
    public Vector2 pitchRange = new Vector2(-80f, 80f);
    

    // ── 内部状态 ──────────────────────────────────────────────

    private float _yaw;
    private float _pitch;
    private Vector3 _lastMousePos;
    private bool  _dragging;

    // ── 聚焦（FocusOnNode）保存/恢复 ──────────────────────────
    private float _savedYaw;
    private float _savedPitch;
    private float _savedRadius;
    private bool  _hasSavedState;

    // ── 沿法线回退 ──────────────────────────────────────────

    // ── 默认轨道（供外部回到初始姿态） ─────────────────────────
    private float _defaultOrbitRadius;


    void OnEnable()
    {
        _yaw   = initialYaw;
        _pitch = initialPitch;
        _defaultOrbitRadius = orbitRadius;
    }

    void Update()
    {
        if(IsLock)
        {
            _dragging = false;
            return;
        }

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

    public void ApplyOrbit()
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
        _yaw   = Mathf.Atan2(-direction.x, -direction.z) * Mathf.Rad2Deg;
        _pitch = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) * Mathf.Rad2Deg;
        _pitch = Mathf.Clamp(_pitch, pitchRange.x, pitchRange.y);
        ApplyOrbit();
    }

    /// <summary>
    /// 根据方向向量计算轨道上的位置（不修改内部 yaw/pitch）
    /// </summary>
    public Vector3 GetOrbitPosition(Vector3 direction)
    {
        if (planetCenter == null) return transform.position;
        Vector3 dir = direction.normalized;
        float yaw   = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg;
        float pitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(pitch, pitchRange.x, pitchRange.y);
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        return planetCenter.position + rot * Vector3.back * orbitRadius;
    }

    /// <summary>
    /// 获取 initialYaw / initialPitch / 默认半径 决定的初始轨道位置
    /// </summary>
    public Vector3 GetDefaultOrbitPosition()
    {
        if (planetCenter == null) return transform.position;
        Quaternion rot = Quaternion.Euler(initialPitch, initialYaw, 0f);
        return planetCenter.position + rot * Vector3.back * _defaultOrbitRadius;
    }

    /// <summary>
    /// 根据当前实际位置同步内部 yaw/pitch（DOTween 移动摄像机后调用，避免 ApplyOrbit 回弹）
    /// </summary>
    public void SyncFromPosition()
    {
        if (planetCenter == null) return;
        Vector3 offset = transform.position - planetCenter.position;
        _yaw   = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
        _pitch = Mathf.Asin(Mathf.Clamp(offset.y / offset.magnitude, -1f, 1f)) * Mathf.Rad2Deg;
        _pitch = Mathf.Clamp(_pitch, pitchRange.x, pitchRange.y);
        orbitRadius = Mathf.Clamp(offset.magnitude, radiusRange.x, radiusRange.y);
    }

    // ── 聚焦节点（拉近 / 恢复）────────────────────────────────

    /// <summary>
    /// 保存当前轨道状态，供后续 RestoreOrbit 恢复
    /// </summary>
    public void SaveOrbitState()
    {
        _savedYaw    = _yaw;
        _savedPitch  = _pitch;
        _savedRadius = orbitRadius;
        _hasSavedState = true;
    }

    /// <summary>
    /// 平滑聚焦到节点：沿球面法线方向拉到最近距离，正对该点
    /// </summary>
    public void FocusOnNode(Vector3 nodeWorldPos, float duration, System.Action onComplete = null)
    {
        if (planetCenter == null) return;

        transform.DOKill(false);

        Vector3 surfaceNormal = (nodeWorldPos - planetCenter.position).normalized;
        float targetRadius = radiusRange.x;

        Vector3 targetPos   = planetCenter.position + surfaceNormal * targetRadius;
        Quaternion targetRot = Quaternion.LookRotation(planetCenter.position - targetPos, Vector3.up);

        var seq = DOTween.Sequence();
        seq.Join(transform.DOMove(targetPos, duration).SetEase(Ease.InOutSine));
        seq.Join(transform.DORotateQuaternion(targetRot, duration).SetEase(Ease.InOutSine));
        seq.OnComplete(() =>
        {
            SyncFromPosition();
            orbitRadius = targetRadius;
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 平滑恢复到 SaveOrbitState 保存的轨道状态
    /// </summary>
    public void RestoreOrbit(float duration, System.Action onComplete = null)
    {
        if (!_hasSavedState || planetCenter == null) return;

        transform.DOKill(false);

        Vector3 targetPos = planetCenter.position
                          + Quaternion.Euler(_savedPitch, _savedYaw, 0f) * Vector3.back * _savedRadius;
        Quaternion targetRot = Quaternion.LookRotation(planetCenter.position - targetPos, Vector3.up);

        var seq = DOTween.Sequence();
        seq.Join(transform.DOMove(targetPos, duration).SetEase(Ease.InOutSine));
        seq.Join(transform.DORotateQuaternion(targetRot, duration).SetEase(Ease.InOutSine));
        seq.OnComplete(() =>
        {
            _yaw        = _savedYaw;
            _pitch      = _savedPitch;
            orbitRadius = _savedRadius;
            _hasSavedState = false;
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 从当前摄像机位置沿法线方向回退到默认轨道距离
    /// </summary>
    public void ReturnFromFocus(float duration, System.Action onComplete = null)
    {
        if (planetCenter == null) return;

        transform.DOKill(false);

        // 从当前实际位置反算法线方向，不依赖 _lastFocusNormal
        Vector3 currentNormal = (transform.position - planetCenter.position).normalized;
        float returnRadius = focusReturnRadius;
        Vector3 targetPos = planetCenter.position + currentNormal * returnRadius;
        Quaternion targetRot = Quaternion.LookRotation(planetCenter.position - targetPos, Vector3.up);

        var seq = DOTween.Sequence();
        seq.Join(transform.DOMove(targetPos, duration).SetEase(Ease.InOutSine));
        seq.Join(transform.DORotateQuaternion(targetRot, duration).SetEase(Ease.InOutSine));
        seq.OnComplete(() =>
        {
            SyncFromPosition();
            onComplete?.Invoke();
        });
    }
}
