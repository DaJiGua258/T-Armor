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
        public WeaponStateEnum WeaponState;
        public int MaxAmmo;
        public BindableProperty<int> CurMaxAmmo = new BindableProperty<int>();  // 当前所有的弹药
        public int MaxMagazine;
        public BindableProperty<int> CurMagazine = new BindableProperty<int>();
        public float ReloadTime;
        public int BulletSpeed;
        public int BulletDamage;
        public int Rpm;

        // Mod 系统：已装备的 Mod（以物品形式存储，ModData 在 item.ModData 中）
        public List<ItemDataModel> EquippedMods = new();

        // 基础配置缓存，供 RecalculateStats 重算时使用
        private WeaponConfig _baseConfig;

        public WeaponDataModel(WeaponConfig weaponConfig)
        {
            this.TypeEnum = TypeEnum.Weapon;
            this.InstanceId.Value = GetInstanceId((int)weaponConfig.WeaponType, _weaponCounter);
            _weaponCounter++;

            this.WeaponType = weaponConfig.WeaponType;
            this.WeaponState = WeaponStateEnum.Idle;

            //
            this.MaxAmmo = weaponConfig.MaxAmmo;
            this.CurMaxAmmo.Value = weaponConfig.CurAmmo;

            //
            this.MaxMagazine = weaponConfig.MaxMagazine;
            this.CurMagazine.Value = weaponConfig.MaxMagazine;


            this.ReloadTime = weaponConfig.ReloadTime;
            this.BulletSpeed = weaponConfig.BulletSpeed;
            this.BulletDamage = weaponConfig.BulletDamage;
            this.Rpm = weaponConfig.Rpm;

            // 缓存基础配置
            _baseConfig = weaponConfig;

            // 初始化 6 个空 Mod 槽位
            for (int i = 0; i < 6; i++)
                EquippedMods.Add(new ItemDataModel());
        }

        /// <summary>
        /// 根据基础配置 + 已装备 Mod 重新计算武器属性
        /// </summary>
        public void RecalculateStats()
        {
            if (_baseConfig == null) return;

            // 从基础配置还原
            MaxAmmo = _baseConfig.MaxAmmo;
            CurMaxAmmo.Value = _baseConfig.CurAmmo;
            MaxMagazine = _baseConfig.MaxMagazine;
            CurMagazine.Value = _baseConfig.MaxMagazine;
            ReloadTime = _baseConfig.ReloadTime;
            BulletSpeed = _baseConfig.BulletSpeed;
            BulletDamage = _baseConfig.BulletDamage;
            Rpm = _baseConfig.Rpm;

            // 遍历装备的 Mod 应用词条
            foreach (var item in EquippedMods)
            {
                if (item.ModData == null) continue;
                foreach (var entry in item.ModData.Entries)
                {
                    switch (entry.Target)
                    {
                        case StatName.BulletDamage:    ApplyMod(ref BulletDamage, entry.Operator, entry.Value); break;
                        case StatName.MaxMagazine:     ApplyMod(ref MaxMagazine, entry.Operator, entry.Value); break;
                        case StatName.BulletSpeed:     ApplyMod(ref BulletSpeed, entry.Operator, entry.Value); break;
                        case StatName.Rpm:             ApplyMod(ref Rpm, entry.Operator, entry.Value); break;
                        case StatName.ReloadTime:      ApplyMod(ref ReloadTime, entry.Operator, entry.Value); break;
                    }
                }
            }
        }

        private static void ApplyMod(ref int stat, ModOp op, float value)
        {
            if (op == ModOp.Add)
                stat += (int)value;
            else
                stat = (int)(stat * (1f + value));
        }

        private static void ApplyMod(ref float stat, ModOp op, float value)
        {
            if (op == ModOp.Add)
                stat += value;
            else
                stat *= (1f + value);
        }
    }
}