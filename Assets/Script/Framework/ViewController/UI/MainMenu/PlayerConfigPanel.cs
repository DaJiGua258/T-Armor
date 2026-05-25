using System.Collections.Generic;
using DG.Tweening;
using QFramework.Enum;
using QFramework.Model;
using QFramework.System;
using QFramework.ViewController.UI.SupportConfig;
using QFramework.ViewController.UI.WeaponConfig;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class PlayerConfigPanel : AbstractBasePanel
    {
        [SerializeField] private Button _leftHangerBtn;
        [SerializeField] private Button _leftSideBtn;
        [SerializeField] private Button _rightHangerBtn;
        [SerializeField] private Button _rightSideBtn;

        private UIHighlight _leftHangerHighlight;
        private UIHighlight _leftSideHighlight;
        private UIHighlight _rightHangerHighlight;
        private UIHighlight _rightSideHighlight;

        [SerializeField] private RawImage _playerRawImage;
        private RenderTexture _rt;

        [SerializeField] private Transform _weaponListNode;

        private WeaponSelectList _weaponSelect;
        private IPlayerSystem _playerSystem;
        private IWeaponConfigModel _weaponConfigModel;

        private enum WeaponSlot
        {
            LeftSide,
            RightSide,
            LeftHanger,
            RightHanger,
        }

        private WeaponSlot _currentSlot;

        [Header("滑入滑出")]
        [SerializeField] private float _slideDuration = 0.35f;
        [SerializeField] private float _panelHiddenY = -600f;

        private float _hiddenY;
        private bool _isListShowing;
        private Tween _listTween;

        private Tween _panelTween;
        private RectTransform _rectTransform;

        [Header("热键物品")]
        [SerializeField] private Slot[] _hotkeySlots = new Slot[3];
        [SerializeField] private Transform _supportListNode;
        private SupportSelectList _supportSelect;
        private ItemTypeEnum[] _selectedHotkeyItems = new ItemTypeEnum[3];
        private int _currentHotkeySlot;
        private bool _isSupportListShowing;
        private float _supportHiddenY;
        private Tween _supportListTween;

        private void Awake()
        {
            _playerSystem = this.GetSystem<IPlayerSystem>();
            _weaponConfigModel = this.GetModel<IWeaponConfigModel>();
            _weaponSelect = _weaponListNode.GetComponent<WeaponSelectList>();
            _hiddenY = _weaponListNode.localPosition.y;
            _supportHiddenY = _supportListNode.localPosition.y;
            _rectTransform = transform as RectTransform;

            _leftSideBtn.onClick.AddListener(() => OnSlotBtnClick(WeaponSlot.LeftSide));
            _rightSideBtn.onClick.AddListener(() => OnSlotBtnClick(WeaponSlot.RightSide));
            _leftHangerBtn.onClick.AddListener(() => OnSlotBtnClick(WeaponSlot.LeftHanger));
            _rightHangerBtn.onClick.AddListener(() => OnSlotBtnClick(WeaponSlot.RightHanger));

            _leftSideHighlight = SetupSlotHighlight(_leftSideBtn, WeaponSlot.LeftSide);
            _rightSideHighlight = SetupSlotHighlight(_rightSideBtn, WeaponSlot.RightSide);
            _leftHangerHighlight = SetupSlotHighlight(_leftHangerBtn, WeaponSlot.LeftHanger);
            _rightHangerHighlight = SetupSlotHighlight(_rightHangerBtn, WeaponSlot.RightHanger);

            // 热键槽初始化
            for (int i = 0; i < _hotkeySlots.Length; i++)
            {
                var index = i;
                var btn = _hotkeySlots[i].GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnHotkeySlotClick(index));
                }
            }

            _supportSelect = _supportListNode.GetComponent<SupportSelectList>();
        }

        public override void OnShow()
        {
            UpdatePlayerRT();

            // 从隐藏位置滑入
            _panelTween?.Kill();
            var pos = _rectTransform.anchoredPosition;
            pos.y = _panelHiddenY;
            _rectTransform.anchoredPosition = pos;
            _panelTween = _rectTransform.DOAnchorPosY(0, _slideDuration).SetEase(Ease.OutSine);
        }

        public override void Hide()
        {
            OnHide();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);

            _isListShowing = false;
            _panelTween?.Kill();
            _panelTween = _rectTransform.DOAnchorPosY(_panelHiddenY, _slideDuration).SetEase(Ease.InSine)
                .OnComplete(() => { gameObject.SetActive(false); });
        }

        private void OnEnable()
        {
            UpdatePlayerRT();
        }

        private void Start()
        {
            if (_playerSystem.PlayerWeapon.Left.Value == null)
                _playerSystem.InitPlayerWeapon();
            if (_playerSystem.PlayerWeapon.HangerLeft.Value == null)
                _playerSystem.InitHangerWeapon();

            if (_weaponSelect != null)
                _weaponSelect.OnWeaponConfirmed += OnWeaponSelected;
            if (_supportSelect != null)
                _supportSelect.OnItemConfirmed += OnSupportItemSelected;

            // 初始化热键槽显示
            for (int i = 0; i < _selectedHotkeyItems.Length; i++)
                _selectedHotkeyItems[i] = ItemTypeEnum.None;

            UpdateHotkeySlotUI();

            UpdatePlayerRT();
        }

        private void UpdatePlayerRT()
        {
            if (RenderTextureManager.Instance != null)
            {
                _rt = RenderTextureManager.Instance.PlayerRT;
                _playerRawImage.texture = _rt;
            }
        }

        private void OnSlotBtnClick(WeaponSlot slot)
        {
            // 先关支援物品列表
            if (_isSupportListShowing)
            {
                _supportListTween?.Kill();
                _supportListTween = _supportListNode.DOLocalMoveY(_supportHiddenY, 0.2f).SetEase(Ease.OutSine)
                    .OnComplete(() => _isSupportListShowing = false);
            }

            var isSameSlot = _currentSlot == slot;
            _currentSlot = slot;
            UpdateSlotHighlights();

            var isHanger = slot is WeaponSlot.LeftHanger or WeaponSlot.RightHanger;

            var playerWeapon = _playerSystem.PlayerWeapon;
            WeaponDataModel currentWeapon = slot switch
            {
                WeaponSlot.LeftSide => playerWeapon.Left.Value,
                WeaponSlot.RightSide => playerWeapon.Right.Value,
                WeaponSlot.LeftHanger => playerWeapon.HangerLeft.Value,
                WeaponSlot.RightHanger => playerWeapon.HangerRight.Value,
            };

            _listTween?.Kill();

            if (_isListShowing && isSameSlot)
            {
                // 点击相同按钮 → 关闭 list
                _listTween = _weaponListNode.DOLocalMoveY(_hiddenY, 0.3f).SetEase(Ease.OutSine)
                    .OnComplete(() => _isListShowing = false);
            }
            else if (_isListShowing)
            {
                // 切换到不同按钮 → 滑出 → 更新内容 → 滑入
                _listTween = _weaponListNode.DOLocalMoveY(_hiddenY, 0.3f).SetEase(Ease.OutSine)
                    .OnComplete(() =>
                    {
                        _weaponSelect.ShowWeapons(isHanger, currentWeapon?.WeaponType ?? WeaponTypeEnum.None);
                        _listTween = _weaponListNode.DOLocalMoveY(0, 0.3f).SetEase(Ease.OutSine);
                    });
            }
            else
            {
                // 列表未显示 → 从隐藏位置滑入
                var pos = _weaponListNode.localPosition;
                pos.y = _hiddenY;
                _weaponListNode.localPosition = pos;

                _weaponSelect.ShowWeapons(isHanger, currentWeapon?.WeaponType ?? WeaponTypeEnum.None);
                _listTween = _weaponListNode.DOLocalMoveY(0, 0.3f).SetEase(Ease.OutSine);
                _isListShowing = true;
            }
        }

        private UIHighlight SetupSlotHighlight(Button btn, WeaponSlot slot)
        {
            var highlight = btn.gameObject.AddComponent<UIHighlight>();
            highlight.Setup(btn.GetComponent<Image>(), btn.GetComponentInChildren<Text>());

            var trigger = btn.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => highlight.SetHighlight(true));
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ =>
            {
                if (_currentSlot != slot)
                    highlight.SetHighlight(false);
            });
            trigger.triggers.Add(exit);

            return highlight;
        }

        private void UpdateSlotHighlights()
        {
            if (_leftSideHighlight != null)
                _leftSideHighlight.SetHighlight(_currentSlot == WeaponSlot.LeftSide);
            if (_rightSideHighlight != null)
                _rightSideHighlight.SetHighlight(_currentSlot == WeaponSlot.RightSide);
            if (_leftHangerHighlight != null)
                _leftHangerHighlight.SetHighlight(_currentSlot == WeaponSlot.LeftHanger);
            if (_rightHangerHighlight != null)
                _rightHangerHighlight.SetHighlight(_currentSlot == WeaponSlot.RightHanger);
        }

        private void OnWeaponSelected(WeaponTypeEnum weaponType)
        {
            var playerWeapon = _playerSystem.PlayerWeapon;

            var config = _currentSlot is WeaponSlot.LeftHanger or WeaponSlot.RightHanger
                ? _weaponConfigModel.GetHangerWeaponConfigModel(weaponType)
                : _weaponConfigModel.GetWeaponConfigModel(weaponType);

            var weaponData = new WeaponDataModel(config);

            switch (_currentSlot)
            {
                case WeaponSlot.LeftSide:
                    playerWeapon.Left.Value = weaponData;
                    break;
                case WeaponSlot.RightSide:
                    playerWeapon.Right.Value = weaponData;
                    break;
                case WeaponSlot.LeftHanger:
                    playerWeapon.HangerLeft.Value = weaponData;
                    break;
                case WeaponSlot.RightHanger:
                    playerWeapon.HangerRight.Value = weaponData;
                    break;
            }

            // 选择后保持 list 开启，不关闭
        }

        #region ----- 热键物品 -------------------------

        private void OnHotkeySlotClick(int slotIndex)
        {
            // 支援列表已打开，且点击相同槽位 → 关闭
            if (_isSupportListShowing && _currentHotkeySlot == slotIndex)
            {
                CloseSupportList();
                return;
            }

            _currentHotkeySlot = slotIndex;

            // 支援列表已打开，点击不同槽位 → 只切换选中
            if (_isSupportListShowing)
            {
                _supportSelect.ShowItems(_selectedHotkeyItems[slotIndex]);
                return;
            }

            // 如果武器列表开着，先关掉
            if (_isListShowing)
            {
                _listTween?.Kill();
                _listTween = _weaponListNode.DOLocalMoveY(_hiddenY, 0.2f).SetEase(Ease.OutSine)
                    .OnComplete(() =>
                    {
                        _isListShowing = false;
                        ShowSupportList();
                    });
                return;
            }

            ShowSupportList();
        }

        private void CloseSupportList()
        {
            _supportListTween?.Kill();
            _supportListTween = _supportListNode.DOLocalMoveY(_supportHiddenY, 0.3f).SetEase(Ease.OutSine)
                .OnComplete(() => _isSupportListShowing = false);
        }

        private void ShowSupportList()
        {
            _supportListTween?.Kill();

            var pos = _supportListNode.localPosition;
            pos.y = _supportHiddenY;
            _supportListNode.localPosition = pos;

            _supportSelect.ShowItems(_selectedHotkeyItems[_currentHotkeySlot]);
            _supportListTween = _supportListNode.DOLocalMoveY(0, 0.3f).SetEase(Ease.OutSine);
            _isSupportListShowing = true;
        }

        private void OnSupportItemSelected(ItemTypeEnum itemType)
        {
            _selectedHotkeyItems[_currentHotkeySlot] = itemType;
            UpdateHotkeySlotUI();
            // 保持列表打开，不关闭
        }

        private void UpdateHotkeySlotUI()
        {
            var model = this.GetModel<IItemConfigModel>();
            for (int i = 0; i < _hotkeySlots.Length; i++)
            {
                var config = _selectedHotkeyItems[i] != ItemTypeEnum.None
                    ? model.GetItemConfig(_selectedHotkeyItems[i])
                    : model.GetItemConfig(ItemTypeEnum.None);

                var tempItem = new ItemDataModel(config);
                _hotkeySlots[i].Bind(tempItem);
                _hotkeySlots[i].UpdateSlot();

                // 只显示icon，隐藏数量文字
                var text = _hotkeySlots[i].GetComponentInChildren<Text>();
                if (text != null)
                    text.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 获取选中的热键物品列表（供开始任务时写入背包系统）
        /// </summary>
        public List<ItemTypeEnum> GetSelectedHotkeyItems()
        {
            var list = new List<ItemTypeEnum>();
            foreach (var item in _selectedHotkeyItems)
                list.Add(item);
            return list;
        }

        #endregion
    }
}
