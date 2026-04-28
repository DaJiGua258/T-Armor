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
            {WeaponTypeEnum.None, new WeaponConfig(WeaponTypeEnum.None, 0, 0, 0, 0, 0, 0)},
            {WeaponTypeEnum.AR, new WeaponConfig(WeaponTypeEnum.AR, 5, 30, 2, 20, 10, 600)},
            {WeaponTypeEnum.LMG, new WeaponConfig(WeaponTypeEnum.LMG, 10, 60, 2, 20, 10, 840)},
            {WeaponTypeEnum.SG, new WeaponConfig(WeaponTypeEnum.SG, 4, 30, 2, 20, 10, 180)},
            {WeaponTypeEnum.MRL, new WeaponConfig(WeaponTypeEnum.MRL, 4, 30, 2, 20, 10, 120)},
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

        // 
        public float ReloadTime;
        public int BulletSpeed;
        public int BulletDamage;
        public int Rpm;

        public WeaponConfig(
            WeaponTypeEnum weaponType, 
            int maxAmmoMultipler, 
            int maxMagazine, 
            float reloadTime, 
            int bulletSpeed, 
            int bulletDamage, 
            int rpm)
        {            
            this.WeaponType = weaponType;
            this.MaxAmmo = maxMagazine * maxAmmoMultipler;
            this.CurAmmo = maxMagazine * maxAmmoMultipler;
            this.MaxMagazine = maxMagazine;
            this.ReloadTime = reloadTime;
            this.BulletSpeed = bulletSpeed;
            this.BulletDamage = bulletDamage;
            this.Rpm = rpm;

        }
    
    }
    
    public enum WeaponStateEnum
    {
        Idle,
        Shooting,
        Reloading,
    }
}