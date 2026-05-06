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
    public enum AimingModeEnum
    {
        Combat,
        Interaction,
    }

    public class AimFrame : BaseUIComponent
    {
        [Header("UI 设置")]
        private RectTransform _rectTransform;
        [SerializeField] private float _frameScale = 1.2f;
        [SerializeField] private Vector2 _defaultFrameSize = new Vector2(200, 200);
        

        [Header("检测设置")]
        [SerializeField] private float _aimRadius = 2f;      // 圆形探测的半径
        [SerializeField] private float _miniRadius = 1f;
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

        private AimingModeEnum _currentMode = AimingModeEnum.Combat;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                _currentMode = _currentMode == AimingModeEnum.Combat ? AimingModeEnum.Interaction : AimingModeEnum.Combat;
                TypeEventSystem.Global.Send(new PlayerEvent.SwitchAimingMode { Mode = _currentMode });
            }

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

            string targetTag = _currentMode == AimingModeEnum.Combat ? "Enemy" : "AimTarget";

            float minDis = float.MaxValue;
            Collider2D closest = null;

            for (int i = 0; i < count; i++)
            {
                if(!_raycastResults[i].collider.CompareTag(targetTag)) continue;

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
                float length = UITool.GetCanvasLength(_aimRadius, Camera.main, UIGameManager.Instance.Canvas);
                _rectTransform.sizeDelta = new Vector2(length, length);
                return;
            }

            // 1. 设置大小：根据目标的 Bounds（世界坐标包围盒）转换
            // 这里假设 bounds 是物体的像素/单位大小，乘以缩放
            Vector2 targetWorldSize = _targetCollider.bounds.size;
            float size = targetWorldSize.x > _miniRadius ?  // 如果目标大于最小半径，则使用目标大小
                targetWorldSize.x * _frameScale 
                : _miniRadius * _frameScale;
            _rectTransform.sizeDelta = new Vector2(size, size); // 100f 通常是 PPU

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
                    Pos = UITool.WorldToCanvasPoint(UIGameManager.Instance.Canvas.transform as RectTransform, targetPos)
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

            // 检测目标 layer，更新子弹的 raycast layerMask
            LayerMask bulletLayerMask;
            if (_targetCollider != null)
            {
                bulletLayerMask = 1 << _targetCollider.gameObject.layer;
            }
            else
            {
                bulletLayerMask = LayerMask.GetMask("Ground");
            }
            
            TypeEventSystem.Global.Send(new WeaponEvent.UpdateBulletLayerMask() { LayerMask = bulletLayerMask });

            // ----- 获取敌人信息类的事件，避免重复执行 -------------------------
            if(_targetCollider == null)
            {
                _enemyInfo.SetEnemyId(-1);
                _lastTargetCollider = null;
                return;
            }
            
            if(_targetCollider != null && _lastTargetCollider == _targetCollider) return;

            var enemy = _targetCollider.GetComponentInParent<AbstractEnemy>();
            int enemyId = enemy.enemyId;
            _enemyInfo.SetEnemyId(enemyId);

            
            TypeEventSystem.Global.Send(new DebugEvent.GetEnemyId() { Id = enemyId });
            TypeEventSystem.Global.Send(new DebugEvent.GetEnemyState() { State = enemy.GetCurrentState() });
            TypeEventSystem.Global.Send(new WeaponEvent.GetTargetRig() { TargetRig = _targetCollider.GetComponent<Rigidbody2D>() });
 
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