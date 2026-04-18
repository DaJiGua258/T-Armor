using System.Collections.Generic;
using DG.Tweening;
using QFramework;
using QFramework.Event;
using Unity.VisualScripting;
using UnityEngine;

namespace QFramework.ViewController.UI
{
    public class AimFrame : AbstractBasePanel // 继承自 MonoBehaviour 或 AbstractBasePanel
    {
        [Header("UI 设置")]
        private RectTransform _rectTransform;
        [SerializeField] private float _frameScale = 1.2f;
        [SerializeField] private Vector2 _defaultFrameSize = new Vector2(100, 100);

        [Header("检测设置")]
        [SerializeField] private float _aimRadius = 2f;      // 圆形探测的半径
        [SerializeField] private float _castDistance = 0.1f; // 投射距离（设为很小的值即等同于原地覆盖检测）
        [SerializeField] private Vector2 _castDirection = Vector2.zero;
        [SerializeField] private LayerMask _layerMask;
        
        // 预分配数组，避免 GC
        private RaycastHit2D[] _raycastResults = new RaycastHit2D[10];

        [Header("目标信息 (仅查看)")]
        [SerializeField] private Collider2D _targetCollider;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        void Update()
        {
            DetectAimTargets();
            UpdateFrameTransform();
            SendEvent();
        }

        /// <summary>
        /// 使用 CircleCastNonAlloc 寻找最近的目标
        /// </summary>
        private void DetectAimTargets()
        {
            Vector2 mouseWorldPos = InputUtility.GetMousePos();
            
            // 执行圆形投射
            int count = Physics2D.CircleCastNonAlloc(
                mouseWorldPos,
                _aimRadius,
                _castDirection,
                _raycastResults,
                _castDistance,
                _layerMask
            );

            if (count == 0)
            {
                _targetCollider = null;
                return;
            }

            float minDis = float.MaxValue;
            Collider2D closest = null;

            for (int i = 0; i < count; i++)
            {
                // 计算目标中心到鼠标的距离
                float curDis = Vector2.Distance(mouseWorldPos, _raycastResults[i].collider.transform.position);
                if (curDis < minDis)
                {
                    minDis = curDis;
                    closest = _raycastResults[i].collider;
                }
            }

            _targetCollider = closest;
        }

        /// <summary>
        /// 更新 UI 框的位置和大小
        /// </summary>
        private void UpdateFrameTransform()
        {
            if (_targetCollider == null)
            {
                // 没目标时，跟随鼠标，恢复默认大小
                _rectTransform.DOMove(Input.mousePosition, 0.1f).SetEase(Ease.Linear);
                _rectTransform.sizeDelta = _defaultFrameSize;
                return;
            }

            // 1. 设置大小：根据目标的 Bounds（世界坐标包围盒）转换
            // 这里假设 bounds 是物体的像素/单位大小，乘以缩放
            Vector2 targetWorldSize = _targetCollider.bounds.size;
            _rectTransform.sizeDelta = targetWorldSize * _frameScale; // 100f 通常是 PPU

            // 2. 设置位置：将目标的世界坐标转为屏幕坐标
            _rectTransform.DOMove(Camera.main.WorldToScreenPoint(_targetCollider.transform.position), 0.1f).SetEase(Ease.Linear);
        }

        /// <summary>
        /// 发送 QFramework 事件
        /// </summary>
        private void SendEvent()
        {
            Vector2 targetPos = 
                (_targetCollider != null) 
                ? (Vector2)_targetCollider.transform.position 
                : InputUtility.GetMousePos();

            TypeEventSystem.Global.Send(new PlayerEvent.UpdateTarget() 
            { 
                Target = targetPos 
            });
        }

        /// <summary>
        /// 调试辅助线
        /// </summary>
        void OnDrawGizmos()
        {
            Vector2 origin = InputUtility.GetMousePos();
            Gizmos.color = (_targetCollider != null) ? Color.green : Color.red;

            // 画出起始圆圈
            Gizmos.DrawWireSphere(origin, _aimRadius);

            // 如果有投射距离，画出终点圆圈和连线
            if (_castDistance > 0.01f)
            {
                Vector2 endPos = origin + _castDirection.normalized * _castDistance;
                Gizmos.DrawLine(origin, endPos);
                Gizmos.DrawWireSphere(endPos, _aimRadius);
            }
            
            // 如果选中了目标，画一条线指向目标中心
            if (_targetCollider != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(origin, _targetCollider.transform.position);
            }
        }
    }
}