using System;
using System.Collections.Generic;
using QFramework.Enum;
using QFramework.Model;
using QFramework.System;
using QFramework.Utility;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public enum EnhanceTarget
    {
        LeftWeapon,
        RightWeapon,
        HangerLWeapon,
        HangerRWeapon,
    }

    public class WeaponEnhanceView : BaseUIComponent, IPointerClickHandler
    {
        [SerializeField] private Text _weaponNameText;
        [SerializeField] private Transform _modRoot;
        [SerializeField] private UIHighlight _highlight;
        [SerializeField] private EnhanceTarget _slotType;
        [SerializeField] private GameObject _rtPrefab;
        [SerializeField] private RawImage _weaponIcon;
        [SerializeField] private float _rotationSpeed = 30f;

        private List<Slot> _slots = new List<Slot>();
        private Action<EnhanceTarget> _onSelected;
        private IPlayerSystem _playerSystem => this.GetSystem<IPlayerSystem>();
        private IWeaponConfigModel _weaponConfig => this.GetModel<IWeaponConfigModel>();
        private Camera _rtCamera;
        private RenderTexture _rt;
        private GameObject _weaponModel;
        private Transform _meshTransform;
        private Transform _shadowTransform;
        private WeaponTypeEnum _currentType = WeaponTypeEnum.None;

        private static Transform _rtRoot;

        private void Awake()
        {
            for (int i = 0; i < _modRoot.childCount; i++)
            {
                var slot = _modRoot.GetChild(i).GetComponent<Slot>();
                _slots.Add(slot);
            }
        }

        private void OnDestroy()
        {
            if (_rt != null)
            {
                _rt.Release();
                _rt = null;
            }
        }

        private void Update()
        {
            if (!_rtCamera || !_rtCamera.gameObject.activeSelf) return;
            var delta = _rotationSpeed * Time.deltaTime;
            if (_meshTransform != null)
                _meshTransform.Rotate(0f, 0, delta);
            if (_shadowTransform != null)
                _shadowTransform.Rotate(0f, 0, delta);
        }

        public void Init(Action<EnhanceTarget> onSelected)
        {
            _onSelected = onSelected;
            Refresh();
        }

        public void Refresh()
        {
            var weapon = TryGetWeapon();
            if (weapon != null)
            {
                if (_weaponNameText != null)
                    _weaponNameText.text = _weaponConfig.GetDisplayName(weapon.WeaponType);
                for (int i = 0; i < _slots.Count; i++)
                {
                    _slots[i].Bind(weapon.EquippedMods[i]);
                    _slots[i].UpdateSlot();
                }
                RefreshRT(weapon.WeaponType);
            }
        }

        private void EnsureRTSetup()
        {
            if (_rtCamera != null) return;

            // 共享根节点，所有 RT 实例放一起便于管理
            if (_rtRoot == null)
            {
                _rtRoot = new GameObject("__GameWeaponRT__").transform;
                _rtRoot.position = new Vector3(0, -5000, 0);
            }

            var inst = Instantiate(_rtPrefab, _rtRoot);
            inst.transform.localPosition = new Vector3((int)_slotType * 10f, 0, 0);

            _rtCamera = inst.GetComponentInChildren<Camera>();
            _rtCamera.gameObject.SetActive(false);

            _rt = new RenderTexture(256, 256, 16);
            _rt.Create();
            _rtCamera.targetTexture = _rt;
        }

        private void RefreshRT(WeaponTypeEnum type)
        {
            if (_rtPrefab == null || _weaponIcon == null) return;

            // 同一把武器，不重建模型（旋转不会重置）
            if (type == _currentType && _weaponModel != null)
                return;

            EnsureRTSetup();

            // 清除旧模型
            if (_weaponModel != null)
            {
                Destroy(_weaponModel);
                _weaponModel = null;
            }

            // 加载武器模型
            var isHanger = _slotType is EnhanceTarget.HangerLWeapon or EnhanceTarget.HangerRWeapon;
            var path = isHanger ? $"Prefab/Weapon/Hanger/{type}" : $"Prefab/Weapon/Slot/{type}";
            var prefab = this.GetUtility<IResourceLoad>().Load<GameObject>(path);
            if (prefab == null) return;

            var prefabChild = _rtCamera.transform.parent.Find("Prefab");
            _weaponModel = Instantiate(prefab, prefabChild);
            foreach (var mb in _weaponModel.GetComponentsInChildren<MonoBehaviour>())
                mb.enabled = false;

            _meshTransform = _weaponModel.transform.Find("Mesh");
            _shadowTransform = _weaponModel.transform.Find("Shadow");

            _currentType = type;

            // 启用 Camera 渲染，RT 赋给 UI
            _rtCamera.gameObject.SetActive(true);
            _weaponIcon.texture = _rt;
        }

        public void SetHighlight(bool active)
        {
            if (_highlight != null)
                _highlight.SetHighlight(active);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onSelected?.Invoke(_slotType);
        }

        public WeaponDataModel TryGetWeapon()
        {
            var pw = _playerSystem.PlayerWeapon;
            return _slotType switch
            {
                EnhanceTarget.LeftWeapon => pw.Left.Value,
                EnhanceTarget.RightWeapon => pw.Right.Value,
                EnhanceTarget.HangerLWeapon => pw.HangerLeft.Value,
                EnhanceTarget.HangerRWeapon => pw.HangerRight.Value,
                _ => null,
            };
        }

    }
}
