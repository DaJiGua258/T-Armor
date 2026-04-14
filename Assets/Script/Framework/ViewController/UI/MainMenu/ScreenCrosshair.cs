using System.Collections;
using System.Collections.Generic;
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

        [Header("其他")]
        

        private Canvas _parentCanvas;
        private Camera _mainCamera;

        private void Awake()
        {
            _parentCanvas = GetComponentInParent<Canvas>();
        
            _container = transform.Find("Container").GetComponent<RectTransform>();

            _horizontalLine = _container.Find("LineH").GetComponent<RectTransform>();
            _verticalLine = _container.Find("LineV").GetComponent<RectTransform>();

            _mainCamera = Camera.main; // 用于世界坐标转屏幕坐标

            InitializeLines();

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
        /// 更新位置（包含吸附逻辑）
        /// </summary>
        private void UpdatePosition()
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 finalScreenPos = mousePos;
            float minDistance = float.MaxValue;
            bool isSnapping = false;

            // 1. 遍历目标点，寻找最近的吸附点
            foreach (var target in _targetTransforms)
            {
                if (target == null) continue;

                // 将世界坐标转换为屏幕坐标
                Vector3 screenPoint = _mainCamera.WorldToScreenPoint(target.position);

                // 检查目标是否在相机前方（防止吸附到背后的物体）
                if (screenPoint.z > 0)
                {
                    Vector2 screenPos2D = new Vector2(screenPoint.x, screenPoint.y);
                    float distance = Vector2.Distance(mousePos, screenPos2D);

                    // 如果在吸附半径内，且是当前最近的点
                    if (distance < _snapRadius && distance < minDistance)
                    {
                        minDistance = distance;
                        finalScreenPos = screenPos2D;
                        isSnapping = true;
                    }
                }
            }

            // 2. 将最终的屏幕位置（鼠标或吸附点）转换为 UI 本地坐标
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
            
            // 3. 可选：吸附时改变颜色作为反馈
            // _horizontalLine.GetComponent<Image>().color = isSnapping ? Color.red : _crosshairColor;
        }
    }
}

