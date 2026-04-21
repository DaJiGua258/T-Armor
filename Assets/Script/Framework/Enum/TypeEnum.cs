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
        AR,  // 突击步枪
        MG,  // 机枪
        SG,  // 霰弹枪
        RL,  // 火箭发射器
    }

    public enum ItemTypeEnum
    {
        None,

        Parts,  // 零件

        // 支援
        Supply_Health,
        Supply_Ammo,

        // 信标
        Beacon_AirStrikes,  // 空袭
        Beacon_AirSupport,  // 空中支援
        Beacon_Shelling,  // 炮击
    }

    public enum EnemyTypeEnum
    {
        None,
        Enemy1,
        Enemy2,
        Enemy3,
        Enemy4,
        Enemy5,
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
        Mission_1,
        Mission_2,
        Mission_3,
        Mission_4,
        Mission_5,
    }

        public enum LevelMissionTypeEnum
    {
        None,
        LevelMission_1,
        LevelMission_2,
    }


}