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
        [SerializeField] private Slot[] _supportSlots = new Slot[3];
        [SerializeField] private Transform _supportListNode;
        private SupportSelectList _supportSelect;
        private SupportTypeEnum[] _selectedSupportItems = new SupportTypeEnum[3];
        private int _currentSupportSlot;
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
            for (int i = 0; i < _supportSlots.Length; i++)
            {
                var index = i;
                var btn = _supportSlots[i].GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnSupportSlotClick(index));
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

            // 从 PlayerSystem 恢复上次的支援物品选中状态
            for (int i = 0; i < _selectedSupportItems.Length && i < _playerSystem.SupportItems.Count; i++)
                _selectedSupportItems[i] = _playerSystem.SupportItems[i].SupportType;

            UpdateSupportSlotUI();

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
                _ => null,
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

            // 保留旧武器的 Mod，继承到新武器上
            var oldMods = CollectWeaponMods(GetCurrentWeapon());

            var config = _currentSlot is WeaponSlot.LeftHanger or WeaponSlot.RightHanger
                ? _weaponConfigModel.GetHangerWeaponConfigModel(weaponType)
                : _weaponConfigModel.GetWeaponConfigModel(weaponType);

            var weaponData = new WeaponDataModel(config);

            // 将旧 Mod 装到新武器空槽中
            int mi = 0;
            for (int m = 0; m < weaponData.EquippedMods.Count && mi < oldMods.Count; m++)
            {
                if (weaponData.EquippedMods[m].ItemType == ItemTypeEnum.None)
                {
                    weaponData.EquippedMods[m] = oldMods[mi];
                    mi++;
                }
            }
            weaponData.RecalculateStats();

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

            // 立即存盘
            this.GetSystem<ILevelSystem>().SavePlayerLoadoutToDisk();
        }

        private WeaponDataModel GetCurrentWeapon()
        {
            var pw = _playerSystem.PlayerWeapon;
            return _currentSlot switch
            {
                WeaponSlot.LeftSide => pw.Left.Value,
                WeaponSlot.RightSide => pw.Right.Value,
                WeaponSlot.LeftHanger => pw.HangerLeft.Value,
                WeaponSlot.RightHanger => pw.HangerRight.Value,
                _ => null,
            };
        }

        private List<ItemDataModel> CollectWeaponMods(WeaponDataModel weapon)
        {
            var mods = new List<ItemDataModel>();
            if (weapon == null) return mods;
            foreach (var mod in weapon.EquippedMods)
            {
                if (mod == null || mod.ItemType == ItemTypeEnum.None) continue;
                mods.Add(mod);
            }
            return mods;
        }

        #region ----- 热键物品 -------------------------

        private void OnSupportSlotClick(int slotIndex)
        {
            // 支援列表已打开，且点击相同槽位 → 关闭
            if (_isSupportListShowing && _currentSupportSlot == slotIndex)
            {
                CloseSupportList();
                return;
            }

            _currentSupportSlot = slotIndex;

            // 支援列表已打开，点击不同槽位 → 只切换选中
            if (_isSupportListShowing)
            {
                _supportSelect.ShowItems(_selectedSupportItems[slotIndex]);
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

            _supportSelect.ShowItems(_selectedSupportItems[_currentSupportSlot]);
            _supportListTween = _supportListNode.DOLocalMoveY(0, 0.3f).SetEase(Ease.OutSine);
            _isSupportListShowing = true;
        }

        private void OnSupportItemSelected(SupportTypeEnum supportType)
        {
            _selectedSupportItems[_currentSupportSlot] = supportType;
            UpdateSupportSlotUI();

            // 立即同步到 PlayerSystem，面板重建后可恢复
            var selectedItems = GetSelectedSupportItems();
            _playerSystem.InitSupportItems(selectedItems);

            // 立即存盘
            this.GetSystem<ILevelSystem>().SavePlayerLoadoutToDisk();
            // 保持列表打开，不关闭
        }

        private void UpdateSupportSlotUI()
        {
            var model = this.GetModel<ISupportConfigModel>();
            for (int i = 0; i < _supportSlots.Length; i++)
            {
                var type = _selectedSupportItems[i];
                var config = type != SupportTypeEnum.None ? model.GetSupportConfig(type) : null;

                var slot = _supportSlots[i];
                var iconImg = slot.transform.Find("Icon_Img")?.GetComponent<Image>();
                var txt = slot.transform.Find("Num_Txt")?.GetComponent<Text>();

                if (config != null)
                {
                    if (iconImg != null)
                    {
                        var sprite = Resources.Load<Sprite>(config.iconPath);
                        iconImg.sprite = sprite;
                        iconImg.gameObject.SetActive(true);
                    }
                    if (txt != null)
                        txt.gameObject.SetActive(false);
                }
                else
                {
                    if (iconImg != null)
                    {
                        iconImg.sprite = null;
                        iconImg.gameObject.SetActive(false);
                    }
                }
            }
        }

        /// <summary>
        /// 获取选中的热键物品列表（供开始任务时写入背包系统）
        /// </summary>
        public List<SupportTypeEnum> GetSelectedSupportItems()
        {
            var list = new List<SupportTypeEnum>();
            foreach (var item in _selectedSupportItems)
                list.Add(item);
            return list;
        }

        #endregion
    }
}
