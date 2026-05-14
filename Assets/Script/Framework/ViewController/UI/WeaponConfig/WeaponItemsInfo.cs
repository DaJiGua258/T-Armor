using QFramework.Enum;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QFramework.ViewController.UI.WeaponConfig
{
    [RequireComponent(typeof(Button))]
    public class WeaponItemsInfo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Text _nameText;
        [SerializeField] private RawImage _iconImage;
        [SerializeField] private Image _nameImage;
        [SerializeField] private UIHighlight _highlight;

        private WeaponTypeEnum _weaponType;
        private bool _isSelected;
        private bool _isHovered;

        public Button Button { get; private set; }
        public WeaponTypeEnum WeaponType => _weaponType;

        private void Awake()
        {
            Button = GetComponent<Button>();

            // 自动初始化高亮组件（未在预制体拖拽绑定时）
            if (_highlight == null && _nameImage != null && _nameText != null)
            {
                _highlight = gameObject.AddComponent<UIHighlight>();
                _highlight.Setup(_nameImage, _nameText);
            }
        }

        public void Init(WeaponTypeEnum weaponType)
        {
            _weaponType = weaponType;
            _nameText.text = weaponType.ToString();
            RefreshState();
        }

        private void Start()
        {
            if (RenderTextureManager.Instance == null) return;
            var rt = RenderTextureManager.Instance.GetRT(_weaponType);
            if (rt != null)
                _iconImage.texture = rt;
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            RefreshState();
        }

        private void RefreshState()
        {
            if (_highlight != null)
                _highlight.SetHighlight(_isSelected || _isHovered);
        }

        private void OnDisable()
        {
            _isHovered = false;
            RefreshState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            RefreshState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            RefreshState();
        }

    }
}
