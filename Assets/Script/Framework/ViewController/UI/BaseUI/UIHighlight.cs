using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    /// <summary>
    /// 通用的高亮显示组件，用于切换 Image.sprite 和 Text.color 的高亮/常态。
    /// 激活态：sprite = null（纯白底），color = Color.black
    /// 常态  ：sprite = 默认边框图，   color = Color.white
    /// </summary>
    public class UIHighlight : MonoBehaviour
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
    }
}
