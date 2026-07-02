using QFramework.Enum;
using QFramework.Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    /// <summary>
    /// 通用的高亮显示组件，用于切换 Image.sprite 和 Text.color 的高亮/常态。
    /// 激活态：sprite = null（纯白底），color = Color.black
    /// 常态  ：sprite = 默认边框图，   color = Color.white
    /// </summary>
    public class UIHighlight : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [SerializeField] private Image _targetImage;
        [SerializeField] private Text _targetText;

        private Sprite _defaultSprite;

        private void Awake()
        {
            CacheDefaultSprite();
        }

        /// <summary>
        /// 代码初始化（用于动态添加组件时替代 Inspector 拖拽）
        /// </summary>
        public void Setup(Image image, Text text)
        {
            _targetImage = image;
            _targetText = text;
            CacheDefaultSprite();
        }

        private void CacheDefaultSprite()
        {
            if (_targetImage != null)
                _defaultSprite = _targetImage.sprite;
        }

        public void SetHighlight(bool highlighted)
        {
            if (_targetImage != null)
                _targetImage.sprite = highlighted ? null : _defaultSprite;
            if (_targetText != null)
                _targetText.color = highlighted ? Color.black : Color.white;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFXFixed(SFXType.ui_hover);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFXFixed(SFXType.ui_click);
            // 将点击事件向上传递（从父级开始），让 WeaponEnhanceView 等也能触发选择逻辑
            if (transform.parent != null)
                ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.pointerClickHandler);
        }
    }
}
