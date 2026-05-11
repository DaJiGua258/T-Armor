using QFramework.Event;
using QFramework.Model;
using QFramework.System;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class AmmoPanel : BaseUIComponent
    {
        [SerializeField] private Text hangerLeftTxt;
        [SerializeField] private Text hangerRightTxt;
        [SerializeField] private Text sideLeftTxt;
        [SerializeField] private Text sideRightTxt;

        private WeaponDataModel _sideLeftData;
        private WeaponDataModel _sideRightData;
        private WeaponDataModel _hangerLeftData;
        private WeaponDataModel _hangerRightData;

        void Start()
        {
            TypeEventSystem.Global.Register<WeaponInfoEvent.Register>(e => RegisterWeaponInfo())
                .UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void RegisterWeaponInfo()
        {
            _sideLeftData = PlayerSystem.PlayerWeapon.Left.Value;
            _sideRightData = PlayerSystem.PlayerWeapon.Right.Value;
            _hangerLeftData = PlayerSystem.PlayerWeapon.HangerLeft.Value;
            _hangerRightData = PlayerSystem.PlayerWeapon.HangerRight.Value;

            if (_sideLeftData != null)
            {
                _sideLeftData.CurMagazine.Register(_ => UpdateAmmo(sideLeftTxt, _sideLeftData));
                _sideLeftData.CurMaxAmmo.Register(_ => UpdateAmmo(sideLeftTxt, _sideLeftData));
                UpdateAmmo(sideLeftTxt, _sideLeftData);
            }

            if (_sideRightData != null)
            {
                _sideRightData.CurMagazine.Register(_ => UpdateAmmo(sideRightTxt, _sideRightData));
                _sideRightData.CurMaxAmmo.Register(_ => UpdateAmmo(sideRightTxt, _sideRightData));
                UpdateAmmo(sideRightTxt, _sideRightData);
            }

            if (_hangerLeftData != null)
            {
                _hangerLeftData.CurMagazine.Register(_ => UpdateAmmo(hangerLeftTxt, _hangerLeftData));
                _hangerLeftData.CurMaxAmmo.Register(_ => UpdateAmmo(hangerLeftTxt, _hangerLeftData));
                UpdateAmmo(hangerLeftTxt, _hangerLeftData);
            }

            if (_hangerRightData != null)
            {
                _hangerRightData.CurMagazine.Register(_ => UpdateAmmo(hangerRightTxt, _hangerRightData));
                _hangerRightData.CurMaxAmmo.Register(_ => UpdateAmmo(hangerRightTxt, _hangerRightData));
                UpdateAmmo(hangerRightTxt, _hangerRightData);
            }
        }

        private void UpdateAmmo(Text txt, WeaponDataModel data)
        {
            if (txt == null) return;
            txt.text = $"{data.CurMagazine.Value:D3} / {data.CurMaxAmmo.Value:D3}";
        }
    }
}
