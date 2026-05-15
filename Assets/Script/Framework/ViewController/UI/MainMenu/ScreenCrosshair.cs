using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;


namespace QFramework.ViewController.MainMenuUI
{
    public class ScreenCrosshair : MonoBehaviour
    {
        [Header("设置")]
        [SerializeField] private Color _crosshairColor = Color.white;
        [SerializeField] private float _lineWidth = 2f;

        [Header("吸附设置")]
        [SerializeField] private float _snapRadius = 50f; // 吸附半径（像素）
        [SerializeField] private List<Transform> _targetTransforms = new List<Transform>(); // 也可以手动拖拽物体

        [Header("引用")]
        [SerializeField] private RectTransform _horizontalLine;
        [SerializeField] private RectTransform _verticalLine;
        [SerializeField] private RectTransform _container;

        [Header("淡入淡出")]
        [SerializeField] private float _fadeDuration = 0.25f;


        private Canvas _parentCanvas;
        private Camera _mainCamera;

        private bool _isLocked;              // 锁定状态：点击 node 后固定到该位置
        private bool _isVisible;             // 可见状态：隐藏时依然跟随鼠标，仅透明
        private Vector3 _lockedWorldPosition; // 锁定的世界坐标（每帧实时转屏幕坐标，实现镜头移动时追踪）
        private Vector2 _lockedScreenPos;     // 锁定时最后计算的屏幕坐标
        private CanvasGroup _containerCg;     // 用于控制容器透明度

        private void Awake()
        {
            _parentCanvas = GetComponentInParent<Canvas>();

            // 从父级面板（LevelSelectPanel）脱离，防止面板切换时被连带禁用
            transform.SetParent(_parentCanvas.transform, true);

            _container = transform.Find("Container").GetComponent<RectTransform>();

            // 获取/添加 Container 的 CanvasGroup，用于透明控制
            _containerCg = _container.GetComponent<CanvasGroup>();
            if (_containerCg == null)
                _containerCg = _container.gameObject.AddComponent<CanvasGroup>();

            _horizontalLine = _container.Find("LineH").GetComponent<RectTransform>();
            _verticalLine = _container.Find("LineV").GetComponent<RectTransform>();

            _mainCamera = Camera.main; // 用于世界坐标转屏幕坐标

            InitializeLines();

            // 初始不可见（透明），但保持 active 以运行 Update 跟随鼠标
            _containerCg.alpha = 0f;
            _isVisible = false;
        }

        private void Update()
        {
            UpdatePosition();
        }

        /// <summary>
        /// 初始化十字准星的粗细和超长长度
        /// </summary>
        public void InitializeLines()
        {
            if (_horizontalLine == null || _verticalLine == null || _container == null)
            {
                Debug.LogError("请在 Inspector 中分配 RectTransform 引用");
                return;
            }

            // 定义一个远超主流显示器分辨率的数值
            float extremeLength = 10000f;

            // 设置横线：宽极长，高为粗细
            _horizontalLine.sizeDelta = new Vector2(extremeLength, _lineWidth);
            // 设置竖线：宽为粗细，高极长
            _verticalLine.sizeDelta = new Vector2(_lineWidth, extremeLength);

            // 设置颜色
            _horizontalLine.GetComponent<Image>().color = _crosshairColor;
            _verticalLine.GetComponent<Image>().color = _crosshairColor;

            // 确保锚点和中心点在中心
            _horizontalLine.anchorMin = _horizontalLine.anchorMax = _horizontalLine.pivot = new Vector2(0.5f, 0.5f);
            _verticalLine.anchorMin = _verticalLine.anchorMax = _verticalLine.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>
        /// 显示十字线（关卡选择界面过渡完成后调用）
        /// </summary>
        public void Show()
        {
            _isLocked = false;
            _isVisible = true;
            _containerCg.DOKill();
            _containerCg.DOFade(1f, _fadeDuration);
        }

        /// <summary>
        /// 隐藏十字线（主菜单等不需要十字线的界面）
        /// 隐藏时保持 active 以继续跟随鼠标，仅透明不可见
        /// </summary>
        public void Hide()
        {
            _isLocked = false;
            _isVisible = false;
            _containerCg.DOKill();
            _containerCg.DOFade(0f, _fadeDuration);
        }

        /// <summary>
        /// 锁定十字线到指定世界坐标的位置（点击 node 后）
        /// 每帧实时转换世界→屏幕，保证镜头移动时十字线始终追踪 node
        /// </summary>
        public void LockAtWorldPosition(Vector3 worldPosition)
        {
            _lockedWorldPosition = worldPosition;
            _isLocked = true;
        }

        /// <summary>
        /// 解锁，恢复跟随鼠标
        /// </summary>
        public void Unlock()
        {
            _isLocked = false;
        }

        /// <summary>
        /// 更新位置（包含吸附与锁定逻辑）
        /// </summary>
        private void UpdatePosition()
        {
            Vector2 finalScreenPos;

            if (_isLocked)
            {
                // 锁定状态：每帧将世界坐标转屏幕坐标，实现镜头移动时持续追踪
                Vector3 screenPoint = _mainCamera.WorldToScreenPoint(_lockedWorldPosition);
                if (screenPoint.z > 0)
                    _lockedScreenPos = screenPoint;
                finalScreenPos = _lockedScreenPos;
            }
            else
            {
                // 正常状态：跟随鼠标 + 吸附到附近目标
                Vector2 mousePos = Input.mousePosition;
                finalScreenPos = mousePos;
                float minDistance = float.MaxValue;

                // 遍历目标点，寻找最近的吸附点
                foreach (var target in _targetTransforms)
                {
                    if (target == null) continue;

                    // 将世界坐标转换为屏幕坐标
                    Vector3 screenPoint = _mainCamera.WorldToScreenPoint(target.position);

                    // 检查目标是否在相机前方（防止吸附到背后的物体）
                    if (screenPoint.z > 0)
                    {
                        Vector2 screenPos2D = screenPoint;
                        float distance = Vector2.Distance(mousePos, screenPos2D);

                        // 如果在吸附半径内，且是当前最近的点
                        if (distance < _snapRadius && distance < minDistance)
                        {
                            minDistance = distance;
                            finalScreenPos = screenPos2D;
                        }
                    }
                }
            }

            // 将最终的屏幕位置（鼠标或吸附点或锁定位置）转换为 UI 本地坐标
            RenderMode renderMode = _parentCanvas.renderMode;
            Camera uiCamera = (renderMode == RenderMode.ScreenSpaceOverlay) ? null : _parentCanvas.worldCamera;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentCanvas.transform as RectTransform,
                finalScreenPos,
                uiCamera,
                out localPoint
            );

            _container.anchoredPosition = localPoint;
        }
    }
}
