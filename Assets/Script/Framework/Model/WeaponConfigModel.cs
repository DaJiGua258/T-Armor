using System.Collections.Generic;
using QFramework.Enum;

namespace QFramework.Model
{
    public interface IWeaponConfigModel : IModel
    {
        public WeaponConfig GetWeaponConfigModel(WeaponTypeEnum weaponType);
    }

    public class WeaponConfigModel : AbstractModel, IWeaponConfigModel
    {
        /// <summary>
        /// 生成在配置中的所有武器数据
        /// </summary>
        private Dictionary<WeaponTypeEnum, WeaponConfig> _weaponModelsConfig = new Dictionary<WeaponTypeEnum, WeaponConfig>()
        {
            {WeaponTypeEnum.None, new WeaponConfig(WeaponTypeEnum.None, 0, 0, 0f, 0, 0, 0f)},
            {WeaponTypeEnum.AR, new WeaponConfig(WeaponTypeEnum.AR, 5, 30, 0.1f, 20, 10, 0.01f)},
            {WeaponTypeEnum.MG, new WeaponConfig(WeaponTypeEnum.MG, 10, 30, 2f, 20, 10, 0.025f)},
            {WeaponTypeEnum.SG, new WeaponConfig(WeaponTypeEnum.SG, 4, 30, 2f, 20, 10, 0.25f)},
            {WeaponTypeEnum.RL, new WeaponConfig(WeaponTypeEnum.RL, 4, 30, 2f, 20, 10, 0.5f)},
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
        public WeaponTypeEnum WeaponType;
        
        // 武器属性

        // 备用弹药
        public int MaxAmmo;
        public int CurAmmo;

        // 弹匣
        public int MaxMagazine;
        public int CurMagazine;

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
            this.WeaponType = weaponType;
            this.MaxAmmo = maxMagazine * maxAmmoMultipler;
            this.CurAmmo = maxMagazine * maxAmmoMultipler;
            this.MaxMagazine = maxMagazine;
            this.CurMagazine = maxMagazine;
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