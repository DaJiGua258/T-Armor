using DG.Tweening;
using QFramework;
using QFramework.Event;
using QFramework.UtilityKit;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : OverrideMonoSingleton<CameraController>
{
    [Header("跟随目标")]
    [SerializeField] private Transform _target;

    [Header("平滑跟随")]
    [SerializeField] private float _followTime = 0.15f;

    [Header("光标偏移")]
    [SerializeField] private float _mouseOffsetThreshold = 3f;
    [SerializeField] private float _mouseMaxOffset = 4f;
    [SerializeField] private float _mouseOffsetSmoothTime = 0.25f;

    private Camera _cam;
    private Vector3 _currentOffset;
    private Vector3 _offsetVelocity;      // 用于 SmoothDamp
    private Vector3 _camVelocity;         // 用于 SmoothDamp

    [Header("震动")]
    [SerializeField] private float _shakeDuration = 0.3f;
    [SerializeField] private int _shakeVibrato = 20;

    private Vector3 _shakeOffset;
    private Tweener _shakeTweener;

    protected override void Awake()
    {
        base.Awake();
        _cam = GetComponent<Camera>() ?? Camera.main;
    }

    void Start()
    {
        TypeEventSystem.Global.Register<ShakeCamera>(e => Shake(e.strength))
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    public void InitCameraTarget(Transform target)
    {
        _target = target;
        // 初始化时直接对齐，避免开局摄像机飞过来
        if (target)
        {
            transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);
        }
    }

    void LateUpdate()
    {
        if (!_target) return;

        Vector3 targetPos = _target.position;
        Vector3 mouseWorld = InputUtility.GetMousePos();

        // 计算鼠标偏移
        Vector3 mouseDelta = mouseWorld - targetPos;
        mouseDelta.z = 0f;
        float mouseDist = mouseDelta.magnitude;

        Vector3 desiredOffset = Vector3.zero;
        if (mouseDist > _mouseOffsetThreshold)
        {
            float offsetMag = Mathf.Min(mouseDist - _mouseOffsetThreshold, _mouseMaxOffset);
            desiredOffset = mouseDelta.normalized * offsetMag;
        }

        // ✅ 用 SmoothDamp 平滑偏移（比 Lerp 更自然，有速度连续性）
        _currentOffset = Vector3.SmoothDamp(
            _currentOffset, desiredOffset, ref _offsetVelocity, _mouseOffsetSmoothTime);

        // 目标摄像机位置 + 震动
        Vector3 desiredCamPos = new Vector3(
            targetPos.x + _currentOffset.x + _shakeOffset.x,
            targetPos.y + _currentOffset.y + _shakeOffset.y,
            transform.position.z
        );

        // ✅ 用 SmoothDamp 平滑摄像机位置
        transform.position = Vector3.SmoothDamp(
            transform.position, desiredCamPos, ref _camVelocity, _followTime);
    }

    public void Shake(float strength)
    {
        _shakeTweener?.Kill();
        _shakeOffset = Vector3.zero;

        _shakeTweener = DOTween.Shake(
            () => _shakeOffset,
            x => _shakeOffset = x,
            _shakeDuration,
            strength,
            _shakeVibrato
        ).SetEase(Ease.OutQuad);
    }
}

public enum ShakeCameraMode
{
    Tiny = 5,
    Small = 10,
    Mid = 15,
    Large = 20,
    Huge = 25,
}