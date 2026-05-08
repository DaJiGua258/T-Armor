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

        // 信标
        Marker_AirStrikes,  // 空袭
        Marker_AirSupport,  // 空中支援
        Marker_Artillery,  // 炮击
        Marker_Missile,  // 增援
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
        Pre_EnemyKill,
        Mission_2,
        Mission_3,
        Mission_4,
        Mission_5,
    }

        public enum LevelMissionTypeEnum
    {
        None,
        LevMis_CleaArea,
        LevelMission_2,
    }


}