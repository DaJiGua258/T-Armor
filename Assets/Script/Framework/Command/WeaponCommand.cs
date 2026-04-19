using QFramework.Enum;
using QFramework.Event;
using QFramework.System;
using QFramework.Utility;
using QFramework.ViewController.Player;
using UnityEngine;

namespace QFramework.Command
{
    public class WeaponCommand
    {

        /// <summary>
        /// 初始化武器
        /// </summary>
        public class Init : AbstractCommand
        {
            private IPlayerSystem _playerSystem => this.GetSystem<IPlayerSystem>();

            protected override void OnExecute()
            {
                // 初始化武器数据
                _playerSystem.InitPlayerWeapon();
                
                // 通知注册UI事件
                TypeEventSystem.Global.Send(new RegisterWeaponInfo());
            }
        }

        public class Shoot : AbstractCommand
        {
            private IPlayerSystem _playerSystem => this.GetSystem<IPlayerSystem>();
            private WeaponDataModel _weaponData;

            public static Shoot Instance = new();

            public Shoot Init(WeaponDataModel weaponData)
            {
                _weaponData = weaponData;
                return this;
            }

            protected override void OnExecute()
            {
                _weaponData.CurMagazine.Value--;  // 当前弹匣弹药

            }
        }

        public class Reload : AbstractCommand
        {
            private WeaponDataModel _weaponData;

            public Reload(WeaponDataModel weaponData)
            {
                _weaponData = weaponData;
            }
            
            protected override void OnExecute()
            {
                if (_weaponData == null
                    || _weaponData.IsReloading
                    || _weaponData.CurMaxAmmo.Value <= 0
                    || _weaponData.CurMagazine.Value >= _weaponData.MaxMagazine)
                {
                    return;
                }

                _weaponData.IsReloading = true;

                this.GetUtility<ITimerUtility>().AddOnce(() =>
                {
                    int needReloadCount = _weaponData.MaxMagazine - _weaponData.CurMagazine.Value;
                    int reloadCount = Mathf.Min(needReloadCount, _weaponData.CurMaxAmmo.Value);

                    _weaponData.CurMaxAmmo.Value -= reloadCount;
                    _weaponData.IsReloading = false;
                    _weaponData.CurMagazine.Value += reloadCount;
                },
                _weaponData.ReloadTime
                );

            }
        }
        
        


    }
}
