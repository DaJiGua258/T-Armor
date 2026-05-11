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

        private WeaponTypeEnum _weaponType;
        private Sprite _uiFrameSprite;
        private bool _isSelected;
        private bool _isHovered;

        public Button Button { get; private set; }
        public WeaponTypeEnum WeaponType => _weaponType;

        private void Awake()
        {
            Button = GetComponent<Button>();
            _uiFrameSprite = _nameImage.sprite;
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
            if (_isSelected || _isHovered)
            {
                _nameImage.sprite = null;
                _nameText.color = Color.black;
            }
            else
            {
                _nameImage.sprite = _uiFrameSprite;
                _nameText.color = Color.white;
            }
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
