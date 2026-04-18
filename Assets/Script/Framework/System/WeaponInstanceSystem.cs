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

            _weaponDataCache.Add(weaponData.InstanceId.Value, weaponData);
            return weaponData.InstanceId.Value;
        }

        /// <summary>
        /// 向缓存添加已存在的武器数据
        /// </summary>
        public void AddExistingWeapon(WeaponDataModel weaponData)
        {
            _weaponDataCache.Add(weaponData.InstanceId.Value, weaponData);
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
        public int MaxAmmo;
        public BindableProperty<int> CurrentAmmo = new BindableProperty<int>();  // 当前所有的弹药
        public int MaxMagazine;
        public BindableProperty<int> CurrentMagazine = new BindableProperty<int>();
        public float ReloadTime;
        public int BulletSpeed;
        public int BulletDamage;
        public float ShootingInterval;
        public bool IsReloading = false;

        public WeaponDataModel(WeaponConfig weaponConfig)
        {
            this.TypeEnum = TypeEnum.Weapon;
            this.InstanceId.Value = GetInstanceId((int)weaponConfig.WeaponType, _weaponCounter);
            _weaponCounter++;
            
            this.WeaponType = weaponConfig.WeaponType;
            this.MaxAmmo = weaponConfig.MaxAmmo;
            this.CurrentAmmo.Value = weaponConfig.CurrentAmmo;
            this.MaxMagazine = weaponConfig.MaxMagazine;
            this.CurrentMagazine.Value = weaponConfig.CurrentMagazine;
            this.ReloadTime = weaponConfig.ReloadTime;
            this.BulletSpeed = weaponConfig.BulletSpeed;
            this.BulletDamage = weaponConfig.BulletDamage;
            this.ShootingInterval = weaponConfig.ShootingInterval;
            this.IsReloading = false;
        }
    }
}