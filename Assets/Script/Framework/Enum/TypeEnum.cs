namespace QFramework.Enum
{
    public enum TypeEnum
    {
        None,
        Weapon,
        Equipment,
        Item,
        Enemy,
        Mission,
    }

    public enum WeaponTypeEnum
    {
        None,

        // 轻型武器
        AR,  // 突击步枪
        HSA,  // 半自动加农炮
        LMG,  // 机枪
        SG,  // 霰弹枪

        // 重型武器
        MRL,  // 火箭发射器

        // 吊架武器
        VML,  // 垂直导弹发射器
        MTT,  // 机载自动炮台
    }

    public enum ItemTypeEnum
    {
        None,

        Parts,  // 零件

        // 支援
        Supply_Health,
        Supply_Ammo,

        // Mod 芯片
        Mod_Damage,
        Mod_Rpm,
        Mod_Homing,
        Mod_Spread,
        Mod_Penetration,
        Mod_Ammo,
        Mod_Reload,
    }

    public enum SupportTypeEnum
    {
        None,
        AirStrikes,
        AirSupport,
        Artillery,
        Missile,
    }

    public enum EnemyTypeEnum
    {
        None,

        // 运输型
        Worker,      // 工蜂（运输型）

        // 战士型
        Warrior_AR,     // 战斗型（步枪）
        Warrior_SN,      // 战斗型（狙击）

        // 哨兵型 
        Sentry_MG,       // 战斗重型（机枪压制）
        Sentry_RL,       // 战斗重型（火箭弹幕）

        // 侦察型
        Spotter_Support,   // 增援呼叫型（信息素） 
        Spotter_Air,
        Spotter_Shelling,

        // 突袭型，轻型炮艇
        Raider,

        // 运输艇
        Dropper_Lit,
        Dropper_Mid
    }

    public enum EquipmentTypeEnum
    {
        None,
        Equipment_1,
        Equipment_2,
        Equipment_3,
        Equipment_4,
        Equipment_5,
    }

    public enum MissionTypeEnum
    {
        None,
        Entry,
        // 注释旧任务类型
        // Pre_EnemyKill,
        // Pre_GetKey,
        // Pre_DestroyBackupHub,   // 破坏备用处理中枢
        // Pri_InvasionSystem,
        // Mission_3,
        // Mission_4,
        // Mission_5,
        ActivateBeacon,  // 激活信标（唯一玩法任务）
        Extraction,
    }

        // 注释旧的关卡任务类型
        // public enum LevelMissionTypeEnum
        // {
        //     None,
        //     LevMis_CleaArea,
        //     LevMis_NodeInvasion,
        //     LevelMission_2,
        // }

        // 新的简化版本
        public enum LevelMissionTypeEnum
        {
            None,
            LevMis_Beacon,  // 激活信标（唯一关卡任务类型）
        }

    // ---- Mod 系统枚举 ----

    public enum ModCategory
    {
        Weapon,
        Body,
    }

    public enum ModOp
    {
        Add,
        Mul,
        Set,  // 直接设定值（用于 bool/开关型词条）
    }

    public enum StatName
    {
        BulletDamage,
        MaxMagazine,
        BulletSpeed,
        Rpm,
        ReloadTime,
        MaxHealth,
        Speed,
        MaxFuel,
        FuelRecovery,
        DashCost,

        Knockback,
        Burn,
        Slow,
        SpreadAngle,
        EnableHoming,
        Penetration,
    }


}