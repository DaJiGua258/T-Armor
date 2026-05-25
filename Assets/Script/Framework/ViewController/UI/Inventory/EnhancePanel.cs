using QFramework.Event;
using QFramework.System;
using UnityEngine;

namespace QFramework.ViewController.UI
{
    public class EnhancePanel : BaseUIComponent
    {
        private WeaponEnhanceView[] _weaponViews;
        private WeaponDataModel _selectedWeapon;
        private BagPanel _bagPanel;

        private IPlayerSystem _playerSystem => this.GetSystem<IPlayerSystem>();

        private void Awake()
        {
            _weaponViews = GetComponentsInChildren<WeaponEnhanceView>();
            _bagPanel = GetComponentInParent<InventoryPanel>()?.GetComponentInChildren<BagPanel>();        }

        private void Start()
        {
            for (int i = 0; i < _weaponViews.Length; i++)
                _weaponViews[i].Init(OnWeaponSelected);

            // 默认选中第一把武器
            if (_weaponViews.Length > 0)
                OnWeaponSelected(EnhanceTarget.LeftWeapon);

            TypeEventSystem.Global.Register<ModsUpdatedEvent>(_ => OnModsUpdated())
                .UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void OnWeaponSelected(EnhanceTarget slotType)
        {
            // 更新高亮
            for (int i = 0; i < _weaponViews.Length; i++)
                _weaponViews[i].SetHighlight((EnhanceTarget)i == slotType);

            // 更新武器信息
            var view = _weaponViews[(int)slotType];
            if (_bagPanel != null)
            {
                _selectedWeapon = view.TryGetWeapon();
                _bagPanel.ShowWeaponInfo(_selectedWeapon);
                _bagPanel.ShowModInfo(null);
            }
        }

        private void OnModsUpdated()
        {
            // 重算所有武器属性
            for (int i = 0; i < _weaponViews.Length; i++)
            {
                var w = _weaponViews[i].TryGetWeapon();
                w?.RecalculateStats();
                _weaponViews[i].Refresh();
            }

            // 刷新背包和信息显示
            if (_bagPanel != null)
            {
                _bagPanel.RefreshSlots();
                _bagPanel.ShowWeaponInfo(_selectedWeapon);
                _bagPanel.ShowModInfo(null);
            }
        }
    }
}
