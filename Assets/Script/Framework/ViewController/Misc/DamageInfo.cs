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

        public static readonly DamageInfo Default = new DamageInfo
        {
            Damage = 0,
            KnockbackValue = 0f,
            BurnValue = 0f,
            SlowValue = 0f,
            AttackDirection = Vector2.zero,
        };

        public DamageInfo(int damage, float knockbackValue, float burnValue, Vector2 attackDirection, float slowValue = 0f)
        {
            Damage = damage;
            KnockbackValue = knockbackValue;
            BurnValue = burnValue;
            SlowValue = slowValue;
            AttackDirection = attackDirection;
        }
    }
}
