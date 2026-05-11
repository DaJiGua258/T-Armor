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

        private void Awake()
        {
            _playerSystem = this.GetSystem<IPlayerSystem>();
            _weaponConfigModel = this.GetModel<IWeaponConfigModel>();
            _weaponSelect = _weaponListNode.GetComponent<WeaponSelectList>();

            _leftSideBtn.onClick.AddListener(() => OnSlotBtnClick(WeaponSlot.LeftSide));
            _rightSideBtn.onClick.AddListener(() => OnSlotBtnClick(WeaponSlot.RightSide));
            _leftHangerBtn.onClick.AddListener(() => OnSlotBtnClick(WeaponSlot.LeftHanger));
            _rightHangerBtn.onClick.AddListener(() => OnSlotBtnClick(WeaponSlot.RightHanger));
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
            _weaponListNode.gameObject.SetActive(false);

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

            _weaponListNode.gameObject.SetActive(true);
            _weaponSelect.ShowWeapons(isHanger, currentWeapon?.WeaponType ?? WeaponTypeEnum.None);
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

            _weaponListNode.gameObject.SetActive(false);
        }
    }
}
