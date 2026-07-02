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
        public string Description;
        public int MaxAmmo;
        public BindableProperty<int> CurMaxAmmo = new BindableProperty<int>();  // 当前所有的弹药
        public int MaxMagazine;
        public BindableProperty<int> CurMagazine = new BindableProperty<int>();
        public float ReloadTime;
        public int BulletSpeed;
        public int BulletDamage;
        public int Rpm;
        public float KnockbackValue;
        public float BurnValue;
        public float SlowValue;
        public float SpreadAngle;  // 散射角度（0-180°）
        public float Penetration;  // 穿透值
        public bool EnableHoming;  // 子弹追踪开关

        // Mod 系统：已装备的 Mod（以物品形式存储，ModData 在 item.ModData 中）
        public List<ItemDataModel> EquippedMods = new();

        // 基础配置缓存，供 RecalculateStats 重算时使用
        private WeaponConfig _baseConfig;
        public WeaponConfig BaseConfig => _baseConfig;

        // Mod 显示用百分比（key = StatName，value = mod 带来的百分比变化）
        public Dictionary<StatName, float> ModDisplayPct = new();

        public WeaponDataModel(WeaponConfig weaponConfig)
        {
            this.TypeEnum = TypeEnum.Weapon;
            this.InstanceId.Value = GetInstanceId((int)weaponConfig.WeaponType, _weaponCounter);
            _weaponCounter++;

            this.WeaponType = weaponConfig.WeaponType;
            this.WeaponState = WeaponStateEnum.Idle;
            this.Description = weaponConfig.Description;

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
            this.KnockbackValue = weaponConfig.KnockbackValue;
            this.BurnValue = weaponConfig.BurnValue;
            this.SlowValue = weaponConfig.SlowValue;
            this.SpreadAngle = weaponConfig.SpreadAngle;
            this.Penetration = weaponConfig.Penetration;
            this.EnableHoming = false;  // 默认关闭追踪

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
            KnockbackValue = _baseConfig.KnockbackValue;
            BurnValue = _baseConfig.BurnValue;
            SlowValue = _baseConfig.SlowValue;
            SpreadAngle = _baseConfig.SpreadAngle;
            Penetration = _baseConfig.Penetration;
            EnableHoming = false;

            // 收集 Mod 加成：Add 累加，Mul 累加百分比，Set 直接赋值
            int aBulletDamage = 0, aMaxMagazine = 0, aBulletSpeed = 0, aRpm = 0;
            float aReloadTime = 0, aKnockback = 0, aBurn = 0, aSlow = 0, aSpread = 0f, aPenetration = 0f;
            float mBulletDamage = 0, mMaxMagazine = 0, mBulletSpeed = 0, mRpm = 0;
            float mReloadTime = 0, mKnockback = 0, mBurn = 0, mSlow = 0, mSpread = 0f, mPenetration = 0f;

            foreach (var item in EquippedMods)
            {
                if (item.ModData == null) continue;
                foreach (var entry in item.ModData.Entries)
                {
                    switch (entry.Operator)
                    {
                        case ModOp.Add:
                            switch (entry.Target)
                            {
                                case StatName.BulletDamage: aBulletDamage += (int)entry.Value; break;
                                case StatName.MaxMagazine:  aMaxMagazine  += (int)entry.Value; break;
                                case StatName.BulletSpeed:  aBulletSpeed  += (int)entry.Value; break;
                                case StatName.Rpm:          aRpm          += (int)entry.Value; break;
                                case StatName.ReloadTime:   aReloadTime   += entry.Value;       break;
                                case StatName.Knockback:    aKnockback    += entry.Value;       break;
                                case StatName.Burn:         aBurn         += entry.Value;       break;
                                case StatName.Slow:         aSlow         += entry.Value;       break;
                                case StatName.SpreadAngle:  aSpread       += entry.Value;       break;
                                case StatName.Penetration:  aPenetration  += entry.Value;       break;
                            }
                            break;
                        case ModOp.Mul:
                            switch (entry.Target)
                            {
                                case StatName.BulletDamage: mBulletDamage += entry.Value; break;
                                case StatName.MaxMagazine:  mMaxMagazine  += entry.Value; break;
                                case StatName.BulletSpeed:  mBulletSpeed  += entry.Value; break;
                                case StatName.Rpm:          mRpm          += entry.Value; break;
                                case StatName.ReloadTime:   mReloadTime   += entry.Value; break;
                                case StatName.Knockback:    mKnockback    += entry.Value; break;
                                case StatName.Burn:         mBurn         += entry.Value; break;
                                case StatName.Slow:         mSlow         += entry.Value; break;
                                case StatName.SpreadAngle:  mSpread       += entry.Value; break;
                                case StatName.Penetration:  mPenetration  += entry.Value; break;
                            }
                            break;
                        case ModOp.Set:
                            switch (entry.Target)
                            {
                                case StatName.EnableHoming: EnableHoming = entry.Value > 0f; break;
                            }
                            break;
                    }
                }
            }

            // 最终公式：(base + sumAdd) * (1 + sumMul)
            BulletDamage   = Compute(BulletDamage,   aBulletDamage, mBulletDamage);
            MaxMagazine    = Compute(MaxMagazine,    aMaxMagazine,  mMaxMagazine);
            BulletSpeed    = Compute(BulletSpeed,    aBulletSpeed,  mBulletSpeed);
            Rpm            = Compute(Rpm,            aRpm,          mRpm);
            ReloadTime     = Compute(ReloadTime,     aReloadTime,   mReloadTime);
            KnockbackValue = Compute(KnockbackValue, aKnockback,    mKnockback);
            BurnValue      = Compute(BurnValue,      aBurn,         mBurn);
            SlowValue      = Compute(SlowValue,      aSlow,         mSlow);
            SpreadAngle    = Compute(SpreadAngle,    aSpread,       mSpread);
            Penetration    = Compute(Penetration,    aPenetration,  mPenetration);

            // 构建 Mod 显示百分比：Add = addSum/base，Mul = mulSum，直接取词条值
            ModDisplayPct.Clear();
            AddModPct(StatName.BulletDamage, aBulletDamage, _baseConfig.BulletDamage, mBulletDamage);
            AddModPct(StatName.MaxMagazine,  aMaxMagazine,  _baseConfig.MaxMagazine,  mMaxMagazine);
            AddModPct(StatName.BulletSpeed,  aBulletSpeed,  _baseConfig.BulletSpeed,  mBulletSpeed);
            AddModPct(StatName.Rpm,          aRpm,          _baseConfig.Rpm,          mRpm);
            AddModPct(StatName.ReloadTime,   aReloadTime,   _baseConfig.ReloadTime,   mReloadTime);
            AddModPct(StatName.SpreadAngle,  aSpread,       _baseConfig.SpreadAngle,  mSpread);
            AddModPct(StatName.Penetration,  aPenetration,  _baseConfig.Penetration,  mPenetration);
        }

        private void AddModPct(StatName stat, float addSum, float baseVal, float mulSum)
        {
            float pct = 0f;
            if (baseVal != 0f)
                pct += addSum / baseVal;
            pct += mulSum;
            if (Mathf.Abs(pct) > 0.001f)
                ModDisplayPct[stat] = pct;
        }

        private static int Compute(int baseVal, int addSum, float mulSum)
        {
            return Mathf.RoundToInt((baseVal + addSum) * (1f + mulSum));
        }

        private static float Compute(float baseVal, float addSum, float mulSum)
        {
            return (baseVal + addSum) * (1f + mulSum);
        }
    }
}