using System;
using System.Collections.Generic;
using System.Text;
using QFramework.Enum;
using QFramework.Event;
using QFramework.Model;
using QFramework.System;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class BagPanel : BaseUIComponent
    {
        [SerializeField] private Transform _inventoryRoot;
        [SerializeField] private InfoBlock _weaponInfo;
        [SerializeField] private InfoBlock _modInfo;

        private List<Slot> _inventorySlots = new List<Slot>();
        private StringBuilder _sb = new StringBuilder();
        private IWeaponConfigModel _weaponConfig => this.GetModel<IWeaponConfigModel>();

        void Awake()
        {
            for (int i = 0; i < _inventoryRoot.childCount; i++)
            {
                var slot = _inventoryRoot.GetChild(i).GetComponent<Slot>();
                slot.Bind(InvenotrySystem.ItemDataCache[i]);
                _inventorySlots.Add(slot);
            }

            _weaponInfo?.Init();
            _modInfo?.Init();
        }

        void Start()
        {
            RefreshSlots();

            TypeEventSystem.Global.Register<UpdateViewerEvent>(OnUpdateViewer)
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            for (int i = 0; i < InvenotrySystem.ItemDataCache.Count; i++)
            {
                InvenotrySystem.ItemDataCache[i].Count.RegisterOnValueChanged(_ => RefreshSlots());
                InvenotrySystem.ItemDataCache[i].InstanceId.RegisterOnValueChanged(_ => RefreshSlots());
            }
        }

        public void RefreshSlots()
        {
            for (int i = 0; i < _inventorySlots.Count; i++)
                _inventorySlots[i].UpdateSlot();
        }

        public void ShowWeaponInfo(WeaponDataModel w)
        {
            if (_weaponInfo == null) return;

            if (w == null)
            {
                _weaponInfo.Clear();
                return;
            }

            _weaponInfo.Show(
                _weaponConfig.GetDisplayName(w.WeaponType),
                "伤害：\n射速：\n弹匣：\n弹药：\n装填：\n弹速：",
                $"{w.BulletDamage}\n{w.Rpm}\n{w.CurMagazine.Value} / {w.MaxMagazine}\n{w.CurMaxAmmo.Value} / {w.MaxAmmo}\n{w.ReloadTime:F1}s\n{w.BulletSpeed}"
            );
        }

        public void ShowModInfo(ItemDataModel item)
        {
            if (_modInfo == null) return;

            if (item == null || item.ModData == null || item.ModData.Entries.Count == 0)
            {
                _modInfo.Clear();
                return;
            }

            _sb.Clear();
            for (int i = 0; i < item.ModData.Entries.Count; i++)
            {
                if (i > 0) _sb.Append('\n');
                _sb.Append(FormatLabel(item.ModData.Entries[i]));
            }
            string labels = _sb.ToString();

            _sb.Clear();
            for (int i = 0; i < item.ModData.Entries.Count; i++)
            {
                if (i > 0) _sb.Append('\n');
                _sb.Append(FormatValue(item.ModData.Entries[i]));
            }
            string values = _sb.ToString();

            _modInfo.Show(item.name, labels, values);
        }

        private void OnUpdateViewer(UpdateViewerEvent e)
        {
            if (e.itemData != null && e.itemData.ModData != null && e.itemData.ModData.Entries.Count > 0)
                ShowModInfo(e.itemData);
            else
                _modInfo?.Clear();
        }

        private static string FormatLabel(ModEntry entry)
        {
            return entry.Target switch
            {
                StatName.BulletDamage => "伤害：",
                StatName.MaxMagazine => "弹匣容量：",
                StatName.BulletSpeed => "弹速：",
                StatName.Rpm => "射速：",
                StatName.ReloadTime => "装填时间：",
                StatName.MaxHealth => "生命上限：",
                StatName.Speed => "移动速度：",
                StatName.MaxFuel => "燃料上限：",
                _ => entry.Target.ToString()
            };
        }

        private static string FormatValue(ModEntry entry)
        {
            if (entry.Operator == ModOp.Add)
                return $"+{entry.Value:F0}";
            else
                return $"+{entry.Value * 100:F0}%";
        }

        [Serializable]
        public class InfoBlock
        {
            [SerializeField] private Transform _root;
            private Text _nameText;
            private Text _infoText;
            private Text _dataText;

            public void Init()
            {
                if (_root == null) return;
                for (int i = 0; i < _root.childCount; i++)
                {
                    var child = _root.GetChild(i);
                    var text = child.GetComponent<Text>();
                    if (text == null) continue;
                    switch (child.name)
                    {
                        case "Name":  _nameText = text; break;
                        case "Info":  _infoText = text; break;
                        case "Data":  _dataText = text; break;
                    }
                }
            }

            public void Clear()
            {
                if (_nameText != null) _nameText.text = string.Empty;
                if (_infoText != null) _infoText.text = string.Empty;
                if (_dataText != null) _dataText.text = string.Empty;
            }

            public void Show(string name, string info, string data)
            {
                if (_nameText != null) _nameText.text = name;
                if (_infoText != null) _infoText.text = info;
                if (_dataText != null) _dataText.text = data;
            }
        }
    }
}
