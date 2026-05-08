using QFramework.Enum;
using QFramework.Event;
using QFramework.Model;
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
                TypeEventSystem.Global.Send(new WeaponInfoEvent.Register());
            }
        }

        /// <summary>
        /// 初始化吊架武器
        /// </summary>
        public class InitHanger : AbstractCommand
        {
            private IPlayerSystem _playerSystem => this.GetSystem<IPlayerSystem>();

            protected override void OnExecute()
            {
                _playerSystem.InitHangerWeapon();
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
                _weaponData.WeaponState = WeaponStateEnum.Shooting;
                // 防御性保护，避免任何路径把弹匣打成负数
                _weaponData.CurMagazine.Value = Mathf.Max(0, _weaponData.CurMagazine.Value - 1);  // 当前弹匣弹药
                _weaponData.WeaponState = WeaponStateEnum.Idle;
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
                    || _weaponData.WeaponState == WeaponStateEnum.Reloading
                    || _weaponData.CurMaxAmmo.Value <= 0
                    || _weaponData.CurMagazine.Value >= _weaponData.MaxMagazine)
                {
                    return;
                }

                _weaponData.WeaponState = WeaponStateEnum.Reloading;

                int needReloadCount = _weaponData.MaxMagazine - _weaponData.CurMagazine.Value;
                int reloadCount = Mathf.Min(needReloadCount, _weaponData.CurMaxAmmo.Value);

                    
                _weaponData.CurMaxAmmo.Value -= reloadCount;  // 触发换弹UI更新
                    

                this.GetUtility<ITimerUtility>().AddOnce(() =>
                {
                    _weaponData.WeaponState = WeaponStateEnum.Idle;
                    _weaponData.CurMagazine.Value += reloadCount;  // 触发弹药数UI更新
                },
                _weaponData.ReloadTime
                );

            }
        }
        
        


    }
}
