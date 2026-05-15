using DG.Tweening;
using QFramework.Enum;
using QFramework.Model;
using QFramework.System;
using QFramework.ViewController.UI.WeaponConfig;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class PlayerConfigPanel : AbstractBasePanel
    {
        [SerializeField] private Button _leftHangerBtn;
        [SerializeField] private Button _leftSideBtn;
        [SerializeField] private Button _rightHangerBtn;
        [SerializeField] private Button _rightSideBtn;
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

        private void Awake()
        {
            _playerSystem = this.GetSystem<IPlayerSystem>();
            _weaponConfigModel = this.GetModel<IWeaponConfigModel>();
            _weaponSelect = _weaponListNode.GetComponent<WeaponSelectList>();
            _hiddenY = _weaponListNode.localPosition.y;
            _rectTransform = transform as RectTransform;

            _leftSideBtn.onClick.AddListener(() => OnSlotBtnClick(WeaponSlot.LeftSide));
            _rightSideBtn.onClick.AddListener(() => OnSlotBtnClick(WeaponSlot.RightSide));
            _leftHangerBtn.onClick.AddListener(() => OnSlotBtnClick(WeaponSlot.LeftHanger));
            _rightHangerBtn.onClick.AddListener(() => OnSlotBtnClick(WeaponSlot.RightHanger));
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

            _weaponSelect.OnWeaponConfirmed += OnWeaponSelected;

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
            _currentSlot = slot;

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

            if (_isListShowing)
            {
                // 滑出到隐藏位 → 更新内容 → 滑入到 0
                _listTween = _weaponListNode.DOLocalMoveY(_hiddenY, 0.3f).SetEase(Ease.OutSine)
                    .OnComplete(() =>
                    {
                        _weaponSelect.ShowWeapons(isHanger, currentWeapon?.WeaponType ?? WeaponTypeEnum.None);
                        _listTween = _weaponListNode.DOLocalMoveY(0, 0.3f).SetEase(Ease.OutSine);
                    });
            }
            else
            {
                var pos = _weaponListNode.localPosition;
                pos.y = _hiddenY;
                _weaponListNode.localPosition = pos;

                _weaponSelect.ShowWeapons(isHanger, currentWeapon?.WeaponType ?? WeaponTypeEnum.None);
                _listTween = _weaponListNode.DOLocalMoveY(0, 0.3f).SetEase(Ease.OutSine);
                _isListShowing = true;
            }
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

            // 滑出到隐藏位
            _listTween?.Kill();
            _listTween = _weaponListNode.DOLocalMoveY(_hiddenY, 0.3f).SetEase(Ease.OutSine)
                .OnComplete(() => _isListShowing = false);
        }
    }
}
