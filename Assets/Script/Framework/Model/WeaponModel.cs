using System.Collections.Generic;
using QFramework.Enum;

namespace QFramework.Model
{
    public interface IWeaponModel : IModel
    {
        public WeaponConfig GetWeaponConfigModel(WeaponTypeEnum weaponType);
    }

    public class WeaponConfigModel : AbstractModel, IWeaponModel
    {
        /// <summary>
        /// 生成在配置中的所有武器数据
        /// </summary>
        private Dictionary<WeaponTypeEnum, WeaponConfig> _weaponModelsConfig = new Dictionary<WeaponTypeEnum, WeaponConfig>()
        {
            {WeaponTypeEnum.Rifle, new WeaponConfig(WeaponTypeEnum.Rifle, 5, 30, 0.5f, 20, 10, 0.1f)},
            {WeaponTypeEnum.Mech, new WeaponConfig(WeaponTypeEnum.Mech, 10, 30, 0.5f, 20, 10, 0.025f)},
            {WeaponTypeEnum.Shotgun, new WeaponConfig(WeaponTypeEnum.Shotgun, 4, 30, 0.5f, 20, 10, 0.25f)},
            {WeaponTypeEnum.Rocket, new WeaponConfig(WeaponTypeEnum.Rocket, 4, 30, 0.5f, 20, 10, 0.5f)},
        };

        protected override void OnInit()
        {
            
        }

        public WeaponConfig GetWeaponConfigModel(WeaponTypeEnum weaponType)
        {
            return _weaponModelsConfig[weaponType];
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    public class WeaponConfig
    {   
        // 标识
        public int TypeId;
        public WeaponTypeEnum WeaponType;
        
        // 武器属性

        // 备用弹药
        public int MaxAmmo;
        public int CurrentAmmo;

        // 弹匣
        public int MaxMagazine;
        public int CurrentMagazine;

        // 
        public float ReloadTime;
        public int BulletSpeed;
        public int BulletDamage;
        public float ShootingInterval;

        public WeaponConfig(
            WeaponTypeEnum weaponType, 
            int maxAmmoMultipler, 
            int maxMagazine, 
            float reloadTime, 
            int bulletSpeed, 
            int bulletDamage, 
            float shootingInterval)
        {
            this.TypeId = TypeIdSetter.GetTypeId(TypeEnum.Weapon, (int)weaponType);
            
            this.WeaponType = weaponType;
            this.MaxAmmo = maxMagazine * maxAmmoMultipler;
            this.CurrentAmmo = maxMagazine * maxAmmoMultipler;
            this.MaxMagazine = maxMagazine;
            this.CurrentMagazine = maxMagazine;
            this.ReloadTime = reloadTime;
            this.BulletSpeed = bulletSpeed;
            this.BulletDamage = bulletDamage;
            this.ShootingInterval = shootingInterval;
        }
    
    }
    
    public enum WeaponStateEnum
    {
        Idle,
        Shooting,
        Reloading,
    }
}