using QFramework.Enum;
using QFramework.Model;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QFramework.ViewController.UI.SupportConfig
{
    [RequireComponent(typeof(Button))]
    public class SupportItemInfo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Text _nameText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _bgImage;
        [SerializeField] private UIHighlight _highlight;

        private ItemTypeEnum _itemType;
        private bool _isSelected;
        private bool _isHovered;

        public Button Button { get; private set; }
        public ItemTypeEnum ItemType => _itemType;

        private void Awake()
        {
            Button = GetComponent<Button>();
            if (_highlight == null && _bgImage != null && _nameText != null)
            {
                _highlight = gameObject.AddComponent<UIHighlight>();
                _highlight.Setup(_bgImage, _nameText);
            }
        }

        public void Init(ItemTypeEnum itemType)
        {
            _itemType = itemType;
            var config = TArmorArchitecture.Interface.GetModel<IItemConfigModel>().GetItemConfig(itemType);
            _nameText.text = config.name;

            if (!string.IsNullOrEmpty(config.iconPath))
            {
                var sprite = Resources.Load<Sprite>(config.iconPath);
                if (sprite != null && _iconImage != null)
                    _iconImage.sprite = sprite;
            }

            RefreshState();
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
