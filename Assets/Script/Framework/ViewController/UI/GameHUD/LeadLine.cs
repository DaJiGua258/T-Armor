using QFramework.Event;
using QFramework.Manager;
using QFramework.UtilityKit;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    /// <summary>
    /// UGUI Image 连接线 —— 通过距离/角度驱动 RectTransform 实现两点连线
    /// </summary>
    public class UILineConnector : MonoBehaviour
    {
        [Header("线条样式")]
        public float LineWidth = 4f;
        public Color LineColor = Color.white;

        private RectTransform _rect;
        private Vector2 _startPos;
        private Vector2 _endPos;
        private bool _isDirty;

        [Header("引用组件")]
        [SerializeField] private int _pointSize;
        [SerializeField] private RectTransform _start;
        [SerializeField] private RectTransform _line;
        [SerializeField] private RectTransform _end;

        void Awake()
        {
            _rect = _line.GetComponent<RectTransform>();
            _rect.pivot = _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
        }

        void Start()
        {
            TypeEventSystem.Global.Register<GetAimFramePos>(e => SetFrom(e.Pos))
                .UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        void Update()
        {

        }

        void LateUpdate()
        {
            Vector2 localPoint = UITool
                .ScreenToCanvasPoint(UIManager.Instance.Canvas.transform as RectTransform, Input.mousePosition);

            
            SetTo(localPoint);

            // 3. 执行刷新
            if (_isDirty)
            {
                Refresh();
            }
        }

        // ── 公开接口 ─────────────────────────────────────────────────────────

        /// <summary>设置起点（Canvas 本地坐标）</summary>
        public void SetFrom(Vector2 localPoint)
        {
            _start.anchoredPosition = localPoint;
            _start.sizeDelta = new Vector2(_pointSize, _pointSize);
            _startPos = localPoint;
            _isDirty = true;
        }

        /// <summary>设置终点（Canvas 本地坐标）</summary>
        public void SetTo(Vector2 localPoint)
        {
            _end.anchoredPosition = localPoint;
            _end.sizeDelta = new Vector2(_pointSize, _pointSize);
            _endPos = localPoint;
            _isDirty = true;
        }

        /// <summary>同时设置起点和终点</summary>
        public void SetPoints(Vector2 from, Vector2 to)
        {
            _startPos = from;
            _endPos = to;
            _isDirty = true;
        }

        // ── 内部刷新 ─────────────────────────────────────────────────────────

        void Refresh()
        {
            Vector2 delta = _endPos - _startPos;

            _rect.anchoredPosition = (_startPos + _endPos) * 0.5f;                    // 中心点
            _rect.sizeDelta = new Vector2(delta.magnitude, LineWidth);  // 长度 & 粗细
            _rect.localRotation = Quaternion.Euler(0, 0,
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);               // 旋转角度

            _isDirty = false;
        }
        
    }
}