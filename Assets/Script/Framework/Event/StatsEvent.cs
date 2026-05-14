using QFramework.Enum;

namespace QFramework.Event
{
    public class StatsEvent
    {
        public struct OnEnemyKilled
        {
            public EnemyTypeEnum Type;
            public int EnemyId;
        }

        public struct OnDamageDealt
        {
            public int EnemyId;
            public float Damage;
            public EnemyTypeEnum Type;
        }

        public struct OnDamageTaken
        {
            public float Damage;
            public float CurrentHealth;
        }

        public struct OnShotFired
        {
            public WeaponTypeEnum WeaponType;
        }

        public struct OnPlayerDeath { }

        public struct OnMissionCompleted
        {
            public MissionTypeEnum Type;
        }

        public struct OnItemCollected
        {
            public int ItemId;
        }
    }
}
