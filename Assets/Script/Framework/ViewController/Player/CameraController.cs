using QFramework.Enum;
using QFramework;
using UnityEngine;
using QFramework.UtilityKit;
using DG.Tweening;

[RequireComponent(typeof(Camera))]
public class CameraController : OverrideMonoSingleton<CameraController>
{
    [Header("跟随目标")]
    [SerializeField] private Transform _target;

    [Header("平滑跟随")]
    [SerializeField] private float _followTime = 0.5f;

    [Header("光标偏移")]
    [SerializeField] private float _mouseOffsetThreshold = 3f;       // 光标距玩家超过此距离时开始偏移
    [SerializeField] private float _mouseMaxOffset = 4f;       // 镜头最大偏移量
    [SerializeField] private float _mouseOffsetTime = 0.25f; // 偏移平滑速度

    private Camera _cam;
    private Plane _plane;
    private Vector3 _currentOffset;
    private Tweener _cameraTweener;

    protected override void Awake()
    {   
        base.Awake();
        
        _cam = GetComponent<Camera>();
        if (!_cam) _cam = Camera.main;
    }

    public void InitCameraTarget(Transform target)
    {
        _target = target;

        // _cameraTweener = transform.DOMove(transform.position, _followTime)
        //                       .SetAutoKill(false)
        //                       .SetEase(Ease.Linear);
    }

    void LateUpdate()
    {
        if (!_target) return;

        Vector3 targetPos = _target.position;
        Vector3 mouseWorld = InputUtility.GetMousePos();


        // 计算光标相对于玩家的方向与距离
        Vector3 mouseDelta = mouseWorld - targetPos;
        mouseDelta.z = 0f;
        float mouseDist = mouseDelta.magnitude;

        // 超出阈值部分线性映射为偏移量，上限为 _mouseMaxOffset
        Vector3 desiredOffset = Vector3.zero;
        if (mouseDist > _mouseOffsetThreshold)
        {
            float offsetMag = Mathf.Min(mouseDist - _mouseOffsetThreshold, _mouseMaxOffset);
            desiredOffset = mouseDelta.normalized * offsetMag;
        }

        // 平滑过渡偏移量
        _currentOffset = Vector3.Lerp(_currentOffset, desiredOffset, _mouseOffsetTime * Time.deltaTime);
        
        // DOTween.To(() => _currentOffset, 
        //             x => _currentOffset = x, 
        //             desiredOffset, 
        //             _mouseOffsetTime).SetEase(Ease.Linear);

        // 目标镜头世界位置（保持 Z 不变）
        Vector3 desiredCamPos = new Vector3(
            targetPos.x + _currentOffset.x,
            targetPos.y + _currentOffset.y,
            transform.position.z
        );

        // _cameraTweener.ChangeEndValue(desiredCamPos, true).Restart();

        transform.position = desiredCamPos;
    }

    /// <summary>
    /// 获取光标世界位置
    /// </summary>
    /// <param name="referencePos"></param>
    /// <returns></returns>
    private Vector3 GetMouseWorldPosition(Vector3 referencePos)
    {
        // 创建一个平面，用于计算点击位置与角色位置的差值
        _plane = new Plane(Vector3.forward, referencePos);

        // 从屏幕点击位置发射射线，获取点击位置的世界坐标
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);

        // 如果射线与平面相交，则返回交点世界坐标
        if (_plane.Raycast(ray, out float d))
        {
            return ray.GetPoint(d);
        }
        return referencePos;
    }
}
