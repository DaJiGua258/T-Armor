using System.Collections.Generic;
using DG.Tweening;
using QFramework;
using QFramework.Enum;
using QFramework.Event;
using QFramework.Manager;
using QFramework.Utility;
using QFramework.UtilityKit;
using QFramework.ViewController.Enemy;
using QFramework.ViewController.Player;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private RectTransform _selectionBox;
        [SerializeField] private float _interactionBoxSize = 50f;
        [SerializeField] private RectTransform _combatFrame;
        [SerializeField] private RectTransform _interactionFrame;
        private Vector2 _fixedSize;
        private Image _aimImage;

        [Header("检测设置")]
        [SerializeField] private Vector2 _aimSize = new Vector2(2, 2);
        [SerializeField] private float _miniRadius = 1f;
        [SerializeField] private float _castDistance = 0.1f; // 投射距离（设为很小的值即等同于原地覆盖检测）
        [SerializeField] private Vector2 _castDirection = Vector2.zero;
        [SerializeField] private LayerMask _layerMask;

        // 预分配数组，避免 GC
        private RaycastHit2D[] _raycastResults = new RaycastHit2D[10];

        [Header("交互检测设置")]
        [SerializeField] private float _interactionRange = 3f;
        [SerializeField] private LayerMask _interactionLayerMask;
        private Collider2D[] _interactionResults = new Collider2D[16];

        [Header("目标信息 (仅查看)")]
        [SerializeField] private int _enemyId = -1;
        [SerializeField] private Collider2D _targetCollider;
        [SerializeField] private Collider2D _lastTargetCollider;
        [Header("UI 引用")]
        [SerializeField] private EnemyInfo _enemyInfo;

        [Header("旋转设置")]
        private float _interactionRotation = -45f;
        private float _rotationDuration = 0.1f;

        private AimingModeEnum _currentMode = AimingModeEnum.Combat;
        private Quaternion _combatRotation = Quaternion.identity;
        private Quaternion _interactionQuaternion;

        // 对外暴露：当前锁定的交互目标，InteractionController 读取用
        public static GameObject CurrentAimTarget { get; private set; }

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _aimImage = GetComponent<Image>();
            _interactionQuaternion = Quaternion.Euler(0, 0, _interactionRotation);
        }

        void Start()
        {
            float w = UITool.GetCanvasLength(_aimSize.x, Camera.main, UIGameManager.Instance.Canvas);
            float h = UITool.GetCanvasLength(_aimSize.y, Camera.main, UIGameManager.Instance.Canvas);
            _fixedSize = new Vector2(w, h);
            _rectTransform.sizeDelta = _fixedSize;
        }

        void Update()
        {
            if (this.GetUtility<IInputUtility>().GetToggleAimModeInput())
            {
                _currentMode = _currentMode == AimingModeEnum.Combat ? AimingModeEnum.Interaction : AimingModeEnum.Combat;
                TypeEventSystem.Global.Send(new PlayerEvent.SwitchAimingMode { Mode = _currentMode });

                Quaternion targetRot = _currentMode == AimingModeEnum.Combat ? _combatRotation : _interactionQuaternion;
                _selectionBox.DORotate(targetRot.eulerAngles, _rotationDuration).SetEase(Ease.Linear);
            }

            if (_currentMode == AimingModeEnum.Combat)
                DetectCombatTargets();
            else
                DetectInteractionTargets();

            UpdateFrameTransform();
            SendEvent();
        }

        /// <summary>
        /// 战斗模式检测：BoxCast 从鼠标位置 → 找最近敌人 (已有逻辑)
        /// </summary>
        private void DetectCombatTargets()
        {
            // 切到战斗模式时清除交互 UI
            TypeEventSystem.Global.Send(new InteractionEvent.HideDots());
            TypeEventSystem.Global.Send(new InteractionEvent.HidePrompt());

            Vector2 mouseWorldPos = InputUtility.GetMousePos();

            int count = Physics2D.BoxCastNonAlloc(
                mouseWorldPos,
                _aimSize,
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
                string tag = _raycastResults[i].collider.tag;
                if (tag != "Enemy" && tag != "AimTarget") continue;

                float curDis = Vector2.Distance(mouseWorldPos, _raycastResults[i].collider.transform.position);
                if (curDis < minDis)
                {
                    minDis = curDis;
                    closest = _raycastResults[i].collider;
                }
            }

            _targetCollider = closest;
            CurrentAimTarget = closest != null ? closest.gameObject : null;
        }

        /// <summary>
        /// 交互模式检测：
        /// - OverlapCircle 从玩家位置 → dots（玩家周围所有可交互目标）
        /// - BoxCast 从鼠标位置 → 锁定（和战斗模式一样，只锁鼠标附近的）
        /// </summary>
        private void DetectInteractionTargets()
        {
            Vector2 playerPos = PlayerController.Instance.transform.position;
            Vector2 mouseWorldPos = InputUtility.GetMousePos();

            // ── dots：玩家范围 OverlapCircle ──
            int dotCount = Physics2D.OverlapCircleNonAlloc(playerPos, _interactionRange, _interactionResults, _interactionLayerMask);
            InteractionEvent.DotScreenPositions.Clear();

            for (int i = 0; i < dotCount; i++)
            {
                Collider2D col = _interactionResults[i];
                if (!col.CompareTag("AimTarget")) continue;
                if (!IsTargetAllowed(col)) continue;
                InteractionEvent.DotScreenPositions.Add(Camera.main.WorldToScreenPoint(col.transform.position));
            }

            if (InteractionEvent.DotScreenPositions.Count > 0)
                TypeEventSystem.Global.Send(new InteractionEvent.ShowDots());
            else
                TypeEventSystem.Global.Send(new InteractionEvent.HideDots());

            // ── 锁定：鼠标位置 BoxCast（和战斗模式一样）──
            int lockCount = Physics2D.BoxCastNonAlloc(
                mouseWorldPos,
                new Vector2(_interactionRange, _interactionRange),
                0,
                Vector2.zero,
                _raycastResults,
                0,
                _interactionLayerMask
            );

            GameObject closestTarget = null;
            float closestDist = float.MaxValue;
            Vector2 closestScreenPos = Vector2.zero;

            for (int i = 0; i < lockCount; i++)
            {
                Collider2D col = _raycastResults[i].collider;
                if (!col.CompareTag("AimTarget")) continue;
                if (!IsTargetAllowed(col)) continue;

                float curDis = Vector2.Distance(mouseWorldPos, col.transform.position);
                if (curDis < closestDist)
                {
                    closestDist = curDis;
                    closestTarget = col.gameObject;
                    closestScreenPos = Camera.main.WorldToScreenPoint(col.transform.position);
                }
            }

            if (closestTarget != null)
            {
                // 锁定前确认目标在玩家范围内
                float distToPlayer = Vector2.Distance(closestTarget.transform.position, playerPos);
                if (distToPlayer <= _interactionRange)
                {
                    _targetCollider = closestTarget.GetComponent<Collider2D>();
                    CurrentAimTarget = closestTarget;
                    SendShowPrompt(closestTarget, closestScreenPos);
                }
                else
                {
                    _targetCollider = null;
                    CurrentAimTarget = null;
                    TypeEventSystem.Global.Send(new InteractionEvent.HidePrompt());
                }
            }
            else
            {
                _targetCollider = null;
                CurrentAimTarget = null;
                TypeEventSystem.Global.Send(new InteractionEvent.HidePrompt());
            }
        }

        private void SendShowPrompt(GameObject target, Vector2 screenPos)
        {
            string actionText = "";
            string nameText = "";

            PickUpItems pickUp = target.GetComponent<PickUpItems>();
            if (pickUp != null)
            {
                actionText = "拾取";
                nameText = GetPickUpDisplayName(pickUp);
            }
            else
            {
                IInteractable interactable = target.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    actionText = interactable.InteractionText;
                    nameText = interactable.DisplayName;
                }
            }

            if (!string.IsNullOrEmpty(actionText))
            {
                TypeEventSystem.Global.Send(new InteractionEvent.ShowPrompt
                {
                    ActionText = actionText,
                    NameText = nameText,
                    ScreenPosition = screenPos
                });
            }
        }

        private string GetPickUpDisplayName(PickUpItems pickUp)
        {
            switch (pickUp._type)
            {
                case QFramework.Enum.TypeEnum.Weapon:
                    return pickUp._weaponType.ToString();
                case QFramework.Enum.TypeEnum.Item:
                    return this.GetModel<QFramework.Model.IItemConfigModel>()
                        .GetItemConfig(pickUp._pickUpType).name;
                default:
                    return "未知";
            }
        }

        /// <summary>
        /// 持有可放置物体时只显示匹配的放置点，其余全部隐藏；锁定目标始终不显示
        /// </summary>
        private bool IsTargetAllowed(Collider2D col)
        {
            // 锁定目标始终过滤
            Grabbable grabbable = col.GetComponent<Grabbable>();
            if (grabbable != null && grabbable.IsLocked) return false;

            Interactable interactable = col.GetComponent<Interactable>();
            if (interactable != null && interactable.IsLocked) return false;

            var held = InteractionController.HeldGrabbable;
            if (held != null && held.ItemType != GrabbableType.None)
            {
                // 持有可放置物体时，只显示类型匹配的放置点
                return interactable != null
                    && interactable.HasSocket
                    && interactable.AcceptedType == held.ItemType;
            }

            // 未持有物体 / 持有不可放置物体时，所有目标都显示
            return true;
        }

        /// <summary>
        /// 更新 UI 框的位置和大小
        /// </summary>
        private void UpdateFrameTransform()
        {
            if (_currentMode == AimingModeEnum.Interaction)
            {
                // 交互模式：父物体隐藏，用 _interactionFrame
                _aimImage.enabled = false;
                _combatFrame.gameObject.SetActive(false);
                _interactionFrame.gameObject.SetActive(true);
                _selectionBox.gameObject.SetActive(true);
                _selectionBox.sizeDelta = new Vector2(_interactionBoxSize, _interactionBoxSize);

                if (_targetCollider == null)
                    _selectionBox.DOMove(Input.mousePosition, 0.1f).SetEase(Ease.Linear);
                else
                    _selectionBox.DOMove(Camera.main.WorldToScreenPoint(_targetCollider.transform.position), 0.1f).SetEase(Ease.Linear);
                return;
            }

            // ── 战斗模式 ──
            _aimImage.enabled = true;
            _combatFrame.gameObject.SetActive(true);
            _interactionFrame.gameObject.SetActive(false);
            _rectTransform.DOMove(Input.mousePosition, 0.1f).SetEase(Ease.Linear);

            if (_targetCollider == null)
            {
                _selectionBox.gameObject.SetActive(false);
                return;
            }

            _selectionBox.gameObject.SetActive(true);
            Vector2 targetWorldSize = _targetCollider.bounds.size;
            float size = targetWorldSize.x > _miniRadius ?
                targetWorldSize.x * _frameScale
                : _miniRadius * _frameScale;
            _selectionBox.sizeDelta = new Vector2(size, size);
            _selectionBox.DOMove(Camera.main.WorldToScreenPoint(_targetCollider.transform.position), 0.1f).SetEase(Ease.Linear);
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
                Vector2 pos = _currentMode == AimingModeEnum.Interaction
                    ? _selectionBox.anchoredPosition + _rectTransform.anchoredPosition
                    : _rectTransform.anchoredPosition;
                TypeEventSystem.Global.Send(new GetAimFramePos()
                {
                    Pos = pos
                });
            }

            // 执行【返回目标世界空间位置到玩家控制器】事件
            // 交互模式下即使有锁定目标，玩家也朝向鼠标方向而非物体
            Vector2 aimTargetPos = _currentMode == AimingModeEnum.Interaction
                ? InputUtility.GetMousePos()
                : targetPos;
            TypeEventSystem.Global.Send(new PlayerEvent.UpdateTarget()
            {
                Target = aimTargetPos,
                HasTarget = _targetCollider != null
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
            if (enemy != null)
            {
                int enemyId = enemy.enemyId;
                _enemyInfo.SetEnemyId(enemyId);

                TypeEventSystem.Global.Send(new DebugEvent.GetEnemyId() { Id = enemyId });
                TypeEventSystem.Global.Send(new DebugEvent.GetEnemyState() { State = enemy.GetCurrentState() });
                TypeEventSystem.Global.Send(new WeaponEvent.GetTargetRig() { TargetRig = enemy.Rb });
                TypeEventSystem.Global.Send(new WeaponEvent.GetTargetCollider() { TargetCollider = _targetCollider });
            }
            else if (_targetCollider.CompareTag("AimTarget"))
            {
                // AimTarget 从父级获取 Rigidbody2D 提供给武器提前量计算
                var rig = _targetCollider.GetComponentInParent<Rigidbody2D>();
                if (rig != null)
                {
                    TypeEventSystem.Global.Send(new WeaponEvent.GetTargetRig() { TargetRig = rig });
                }
                TypeEventSystem.Global.Send(new WeaponEvent.GetTargetCollider() { TargetCollider = _targetCollider });

                _enemyInfo.SetEnemyId(-1);
            }

            _lastTargetCollider = _targetCollider;
        }

        /// <summary>
        /// 调试辅助线
        /// </summary>
        void OnDrawGizmos()
        {
            Vector2 origin = InputUtility.GetMousePos();
            Gizmos.color = (_targetCollider != null) ? Color.green : Color.red;

            Gizmos.DrawWireCube(origin, _aimSize);
        }
    }
}