using UnityEngine;

namespace QFramework.ViewController.Misc
{
    public struct DamageInfo
    {
        public int Damage;
        public float KnockbackValue;
        public float BurnValue;
        public float SlowValue;
        public Vector2 AttackDirection;
        public int Penetration;  // 有效穿透次数（开枪时由float值掷骰决定）

        public static readonly DamageInfo Default = new DamageInfo
        {
            Damage = 0,
            KnockbackValue = 0f,
            BurnValue = 0f,
            SlowValue = 0f,
            AttackDirection = Vector2.zero,
            Penetration = 0,
        };

        public DamageInfo(int damage, float knockbackValue, float burnValue, Vector2 attackDirection, float slowValue = 0f, int penetration = 0)
        {
            Damage = damage;
            KnockbackValue = knockbackValue;
            BurnValue = burnValue;
            SlowValue = slowValue;
            AttackDirection = attackDirection;
            Penetration = penetration;
        }
    }
}
