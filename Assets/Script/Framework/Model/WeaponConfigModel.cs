using System.Collections.Generic;
using QFramework.Enum;

namespace QFramework.Model
{
    public interface IWeaponConfigModel : IModel
    {
        public WeaponConfig GetWeaponConfigModel(WeaponTypeEnum weaponType);
        public WeaponConfig GetHangerWeaponConfigModel(WeaponTypeEnum weaponType);
        public string GetDisplayName(WeaponTypeEnum weaponType);
        public IReadOnlyDictionary<WeaponTypeEnum, WeaponConfig> WeaponConfigs { get; }
        public IReadOnlyDictionary<WeaponTypeEnum, WeaponConfig> HangerWeaponConfigs { get; }
    }

    public class WeaponConfigModel : AbstractModel, IWeaponConfigModel
    {
        private Dictionary<WeaponTypeEnum, WeaponConfig> _weaponConfig = new();
        private Dictionary<WeaponTypeEnum, WeaponConfig> _hangerWeaponConfig = new();

        protected override void OnInit()
        {
            var weapons = new (WeaponTypeEnum type, string name, int ammoMul, int mag, float reload, int speed, int dmg, int rpm)[]
            {
                (WeaponTypeEnum.None, "", 0, 0, 0, 0, 0, 0),
                (WeaponTypeEnum.AR, "突击步枪", 5, 30, 2, 20, 25, 600),
                (WeaponTypeEnum.LMG, "机枪", 10, 60, 2, 25, 10, 840),
                (WeaponTypeEnum.SG, "霰弹枪", 4, 30, 2, 20, 15, 180),
                (WeaponTypeEnum.MRL, "火箭发射器", 4, 30, 2, 20, 15, 120),
            };
            foreach (var w in weapons)
                _weaponConfig[w.type] = new WeaponConfig(w.type, w.name, w.ammoMul, w.mag, w.reload, w.speed, w.dmg, w.rpm);

            var hanger = new (WeaponTypeEnum type, string name, int ammoMul, int mag, float reload, int speed, int dmg, int rpm)[]
            {
                (WeaponTypeEnum.None, "", 0, 0, 0, 0, 0, 0),
                (WeaponTypeEnum.VML, "垂直导弹", 8, 6, 3, 15, 25, 30),
                (WeaponTypeEnum.MTT, "自动炮台", 4, 40, 4, 30, 5, 300),
            };
            foreach (var w in hanger)
                _hangerWeaponConfig[w.type] = new WeaponConfig(w.type, w.name, w.ammoMul, w.mag, w.reload, w.speed, w.dmg, w.rpm);
        }

        public IReadOnlyDictionary<WeaponTypeEnum, WeaponConfig> WeaponConfigs => _weaponConfig;
        public IReadOnlyDictionary<WeaponTypeEnum, WeaponConfig> HangerWeaponConfigs => _hangerWeaponConfig;

        public WeaponConfig GetWeaponConfigModel(WeaponTypeEnum weaponType)
        {
            return _weaponConfig[weaponType];
        }

        public WeaponConfig GetHangerWeaponConfigModel(WeaponTypeEnum weaponType)
        {
            return _hangerWeaponConfig[weaponType];
        }

        public string GetDisplayName(WeaponTypeEnum weaponType)
        {
            if (_weaponConfig.TryGetValue(weaponType, out var config) ||
                _hangerWeaponConfig.TryGetValue(weaponType, out config))
                return config.DisplayName;
            return weaponType.ToString();
        }
    }

    public class WeaponConfig
    {
        public WeaponTypeEnum WeaponType;
        public string DisplayName;

        public int MaxAmmo;
        public int CurAmmo;
        public int MaxMagazine;
        public float ReloadTime;
        public int BulletSpeed;
        public int BulletDamage;
        public int Rpm;

        public WeaponConfig(
            WeaponTypeEnum weaponType,
            string displayName,
            int maxAmmoMultipler,
            int maxMagazine,
            float reloadTime,
            int bulletSpeed,
            int bulletDamage,
            int rpm)
        {
            this.WeaponType = weaponType;
            this.DisplayName = displayName;
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
