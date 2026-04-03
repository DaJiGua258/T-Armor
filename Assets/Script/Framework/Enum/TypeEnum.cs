namespace QFramework.Enum
{
    public static class TypeIdSetter
    {
        public static int GetTypeId(TypeEnum typeEnum, int type)
        {
            return (int)typeEnum * 100 + type;
        }
    }
    
    /// <summary>
    public enum TypeEnum
    {
        None,
        Weapon,
        Equipment,
        PickUp,
        Enemy,
    }

    public enum WeaponTypeEnum
    {
        None,
        Rifle,
        Mech,
        Shotgun,
        Rocket,
    }

    public enum PickUpTypeEnum
    {
        None,
        Item_1,
        Item_2,
        Item_3,
        Item_4,
        Item_5,
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


}