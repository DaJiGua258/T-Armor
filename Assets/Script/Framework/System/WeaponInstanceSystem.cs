using System.Collections.Generic;
using QFramework.Enum;
using QFramework.Model;
using UnityEngine;

namespace QFramework.System
{
    public interface IWeaponInstanceSystem : ISystem
    {
        public int AddWeapon(WeaponTypeEnum weaponType);
        public void AddExistingWeapon(WeaponDataModel weaponData);
        public WeaponDataModel GetWeaponById(int id);
        public WeaponDataModel RemoveWeaponById(int id);
    }

    public class WeaponInstanceSystem : AbstractSystem, IWeaponInstanceSystem
    {
        /// <summary>
        /// 场景中的所有实例化武器数据缓存，在场景中生成的武器数据都会加载进此处
        /// </summary>
        private Dictionary<int, WeaponDataModel> _weaponDataCache = new Dictionary<int, WeaponDataModel>();

        // 武器数据
        private IWeaponConfigModel _weaponModel => this.GetModel<IWeaponConfigModel>();

        // 玩家运行时数据
        protected override void OnInit()
        {   
            
        }

        /// <summary>
        /// 用于场景中加载的武器，将数据加入缓存
        /// </summary>
        public int AddWeapon(WeaponTypeEnum weaponType)
        {
            var weaponConfig = _weaponModel.GetWeaponConfigModel(weaponType);
            WeaponDataModel weaponData = new WeaponDataModel(weaponConfig);

            _weaponDataCache.Add(weaponData.InstanceId, weaponData);
            return weaponData.InstanceId;
        }

        /// <summary>
        /// 向缓存添加已存在的武器数据
        /// </summary>
        public void AddExistingWeapon(WeaponDataModel weaponData)
        {
            _weaponDataCache.Add(weaponData.InstanceId, weaponData);
        }

        /// <summary>
        /// 通过id获取武器数据
        /// </summary>
        public WeaponDataModel GetWeaponById(int id)
        {
            return _weaponDataCache[id];
        }

        /// <summary>
        /// 通过id移除武器数据
        /// </summary>  
        public WeaponDataModel RemoveWeaponById(int id)
        {
            var weaponData = _weaponDataCache[id];
            _weaponDataCache.Remove(id);
            return weaponData;
        }
    }
    
    public class WeaponDataModel : InstanceType
    {
        private static int _weaponCounter = 0;
        public WeaponTypeEnum WeaponType;
        public BindableProperty<int> MaxAmmo = new BindableProperty<int>();
        public BindableProperty<int> CurrentAmmo = new BindableProperty<int>();
        public BindableProperty<int> MaxMagazine = new BindableProperty<int>();
        public BindableProperty<int> CurrentMagazine = new BindableProperty<int>();
        public BindableProperty<float> ReloadTime = new BindableProperty<float>();
        public BindableProperty<int> BulletSpeed = new BindableProperty<int>();
        public BindableProperty<int> BulletDamage = new BindableProperty<int>();
        public BindableProperty<float> ShootingInterval = new BindableProperty<float>();

        public WeaponDataModel(WeaponConfig weaponConfig)
        {
            this.TypeEnum = TypeEnum.Weapon;
            this.InstanceId = GetInstanceId((int)weaponConfig.WeaponType, _weaponCounter);
            _weaponCounter++;
            
            this.WeaponType = weaponConfig.WeaponType;
            this.MaxAmmo.Value = weaponConfig.MaxAmmo;
            this.CurrentAmmo.Value = weaponConfig.CurrentAmmo;
            this.MaxMagazine.Value = weaponConfig.MaxMagazine;
            this.CurrentMagazine.Value = weaponConfig.CurrentMagazine;
            this.ReloadTime.Value = weaponConfig.ReloadTime;
            
            this.BulletSpeed.Value = weaponConfig.BulletSpeed;
            this.BulletDamage.Value = weaponConfig.BulletDamage;
            this.ShootingInterval.Value = weaponConfig.ShootingInterval;
        }

        public void CopyFrom(WeaponDataModel otherWeaponData)
        {
            WeaponType = otherWeaponData.WeaponType;
            MaxAmmo.Value = otherWeaponData.MaxAmmo.Value;
            CurrentAmmo.Value = otherWeaponData.CurrentAmmo.Value;
            MaxMagazine.Value = otherWeaponData.MaxMagazine.Value;
            CurrentMagazine.Value = otherWeaponData.CurrentMagazine.Value;
            ReloadTime.Value = otherWeaponData.ReloadTime.Value;

            BulletSpeed.Value = otherWeaponData.BulletSpeed.Value;
            BulletDamage.Value = otherWeaponData.BulletDamage.Value;
            ShootingInterval.Value = otherWeaponData.ShootingInterval.Value;
        }

        public void SwapWith(WeaponDataModel otherWeaponData)
        {
            (WeaponType, otherWeaponData.WeaponType) = (otherWeaponData.WeaponType, WeaponType);
            (MaxAmmo.Value, otherWeaponData.MaxAmmo.Value) = (otherWeaponData.MaxAmmo.Value, MaxAmmo.Value);
            (CurrentAmmo.Value, otherWeaponData.CurrentAmmo.Value) = (otherWeaponData.CurrentAmmo.Value, CurrentAmmo.Value);
            (MaxMagazine.Value, otherWeaponData.MaxMagazine.Value) = (otherWeaponData.MaxMagazine.Value, MaxMagazine.Value);
            (CurrentMagazine.Value, otherWeaponData.CurrentMagazine.Value) = (otherWeaponData.CurrentMagazine.Value, CurrentMagazine.Value);
            (ReloadTime.Value, otherWeaponData.ReloadTime.Value) = (otherWeaponData.ReloadTime.Value, ReloadTime.Value);

            (BulletSpeed.Value, otherWeaponData.BulletSpeed.Value) = (otherWeaponData.BulletSpeed.Value, BulletSpeed.Value);
            (BulletDamage.Value, otherWeaponData.BulletDamage.Value) = (otherWeaponData.BulletDamage.Value, BulletDamage.Value);
            (ShootingInterval.Value, otherWeaponData.ShootingInterval.Value) = (otherWeaponData.ShootingInterval.Value, ShootingInterval.Value);
        }
    }
}