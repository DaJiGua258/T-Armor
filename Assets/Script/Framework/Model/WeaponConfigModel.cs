using System;
using System.Collections.Generic;
using QFramework.Enum;
using UnityEngine;

namespace QFramework.Model
{
    public interface IWeaponConfigModel : IModel
    {
        /// <summary>根据武器类型获取手持武器配置</summary>
        public WeaponConfig GetWeaponConfigModel(WeaponTypeEnum weaponType);
        /// <summary>根据武器类型获取挂架武器配置</summary>
        public WeaponConfig GetHangerWeaponConfigModel(WeaponTypeEnum weaponType);
        /// <summary>获取武器显示名称</summary>
        public string GetDisplayName(WeaponTypeEnum weaponType);
        public IReadOnlyDictionary<WeaponTypeEnum, WeaponConfig> WeaponConfigs { get; }
        public IReadOnlyDictionary<WeaponTypeEnum, WeaponConfig> HangerWeaponConfigs { get; }
    }

    public class WeaponConfigModel : AbstractModel, IWeaponConfigModel
    {
        private Dictionary<WeaponTypeEnum, WeaponConfig> _weaponConfig = new();  // 手持武器缓存（Category=Handheld）
        private Dictionary<WeaponTypeEnum, WeaponConfig> _hangerWeaponConfig = new();  // 挂架武器缓存（Category=Hanger）

        /// <summary>从 JSON 加载所有武器配置，按类型分别缓存</summary>
        protected override void OnInit()
        {
            // 从 Config/WeaponConfig.json 反序列化所有武器
            var weapons = ConfigLoader.LoadFromJson<WeaponConfig>("Config/WeaponConfig");
            Debug.Log($"[WeaponConfig] 从 JSON 加载了 {weapons.Count} 个武器");
            // 按手持/挂架分类缓存
            foreach (var w in weapons)
            {
                w.PostLoad();  // 计算 MaxAmmo、CurAmmo 等派生字段
                Debug.Log($"[WeaponConfig]   → WeaponType={w.WeaponType}, Category={w.Category}, DMG={w.BulletDamage}");
                if (w.Category == "Hanger")
                    _hangerWeaponConfig[w.WeaponType] = w;
                else
                    _weaponConfig[w.WeaponType] = w;
            }
            Debug.Log($"[WeaponConfig] 手持={_weaponConfig.Count}, 挂架={_hangerWeaponConfig.Count}");
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

        /// <summary>获取武器的显示名称，支持手持和挂架</summary>
        public string GetDisplayName(WeaponTypeEnum weaponType)
        {
            if (_weaponConfig.TryGetValue(weaponType, out var config) ||
                _hangerWeaponConfig.TryGetValue(weaponType, out config))
                return config.DisplayName;
            return weaponType.ToString();
        }
    }

    [Serializable]
    public class WeaponConfig
    {
        public WeaponTypeEnum WeaponType;
        public string DisplayName;

        // JSON 原始字段
        public int AmmoMul;  // 备弹倍率，MaxAmmo = MaxMagazine * AmmoMul
        public string Category;  // "Handheld" / "Hanger"

        // 派生字段（PostLoad 中计算）
        public int MaxAmmo;
        public int CurAmmo;

        public int MaxMagazine;
        public float ReloadTime;  // 装弹时间（秒）
        public int BulletSpeed;
        public int BulletDamage;
        public int Rpm;  // 每分钟射速

        public float KnockbackValue;  // TODO: 已禁用，归零处理
        public float BurnValue;  // TODO: 已禁用，归零处理
        public float SlowValue;  // 减速幅度

        // JsonUtility 反序列化需要无参构造器
        public WeaponConfig() { }

        /// <summary>反序列化后调用，计算派生字段</summary>
        public void PostLoad()
        {
            MaxAmmo = MaxMagazine * AmmoMul;
            CurAmmo = MaxAmmo;
            KnockbackValue = 0;  // 击退已禁用
            BurnValue = 0;  // 灼烧已禁用
        }

        public WeaponConfig(
            WeaponTypeEnum weaponType,
            string displayName,
            int maxAmmoMultipler,
            int maxMagazine,
            float reloadTime,
            int bulletSpeed,
            int bulletDamage,
            int rpm,
            float slowValue = 0f)
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
            this.SlowValue = slowValue;
        }
    }

    public enum WeaponStateEnum
    {
        Idle,
        Shooting,
        Reloading,
    }
}

