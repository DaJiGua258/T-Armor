using System.Collections.Generic;
using DG.Tweening;
using QFramework;
using QFramework.Event;
using QFramework.Manager;
using QFramework.UtilityKit;
using QFramework.ViewController.Enemy;
using Unity.VisualScripting;
using UnityEngine;

namespace QFramework.ViewController.UI
{
    public class AimFrame : AbstractBasePanel // 继承自 MonoBehaviour 或 AbstractBasePanel
    {
        [Header("UI 设置")]
        private RectTransform _rectTransform;
        [SerializeField] private float _frameScale = 1.2f;
        [SerializeField] private Vector2 _defaultFrameSize = new Vector2(200, 200);

        [Header("检测设置")]
        [SerializeField] private float _aimRadius = 2f;      // 圆形探测的半径
        [SerializeField] private float _castDistance = 0.1f; // 投射距离（设为很小的值即等同于原地覆盖检测）
        [SerializeField] private Vector2 _castDirection = Vector2.zero;
        [SerializeField] private LayerMask _layerMask;
        
        // 预分配数组，避免 GC
        private RaycastHit2D[] _raycastResults = new RaycastHit2D[10];

        [Header("目标信息 (仅查看)")]
        [SerializeField] private int _enemyId = -1;
        [SerializeField] private Collider2D _targetCollider;
        [SerializeField] private Collider2D _lastTargetCollider;
        [Header("UI 引用")]
        [SerializeField] private EnemyInfo _enemyInfo;

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
            int count = Physics2D.BoxCastNonAlloc(
                mouseWorldPos,
                new Vector2(_aimRadius, _aimRadius),
                0,
                Vector2.zero,
                _raycastResults,
                0,
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
                float length = UITool.GetCanvasLength(_aimRadius, Camera.main, UIManager.Instance.Canvas);
                _rectTransform.sizeDelta = new Vector2(length, length);
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
            Vector2 targetPos = (_targetCollider != null) 
                ? (Vector2)_targetCollider.transform.position 
                : InputUtility.GetMousePos();

            // 目标存在时，执行返回目标世界空间的事件
            if(_targetCollider != null) 
            {
                TypeEventSystem.Global.Send(new GetAimFramePos()
                {
                    Pos = UITool.WorldToCanvasPoint(UIManager.Instance.Canvas.transform as RectTransform, targetPos)
                });
            }
            else  // 目标不存在时，执行返回瞄准框的事件
            {
                TypeEventSystem.Global.Send(new GetAimFramePos() 
                { 
                    Pos = _rectTransform.anchoredPosition
                });
            }

            // 执行【返回目标世界空间位置到玩家控制器】事件
            TypeEventSystem.Global.Send(new PlayerEvent.UpdateTarget() 
            { 
                Target = targetPos 
            });

            

            // ----- 获取敌人信息类的事件，避免重复执行 -------------------------
            if(_targetCollider == null)
            {
                _enemyInfo.SetEnemyId(-1);
                _lastTargetCollider = null;
                return;
            }
            
            if(_targetCollider != null && _lastTargetCollider == _targetCollider) return;

            int enemyId = _targetCollider.TryGetComponent<EnemyController>(out var enemy) ? enemy.enemyId : -1;
                _enemyInfo.SetEnemyId(enemyId);
                Debug.Log("Update Enemy Info: " + enemyId);
            
            TypeEventSystem.Global.Send(new DebugEvent.GetEnemyState() { State = enemy.GetCurrentState() });

            _lastTargetCollider = _targetCollider;
        }

        /// <summary>
        /// 调试辅助线
        /// </summary>
        void OnDrawGizmos()
        {
            Vector2 origin = InputUtility.GetMousePos();
            Gizmos.color = (_targetCollider != null) ? Color.green : Color.red;

            Gizmos.DrawWireCube(origin, new Vector2(_aimRadius, _aimRadius));
        }
    }
}