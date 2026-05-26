using System;
using System.Collections.Generic;
using QFramework.Enum;
using UnityEngine;

namespace QFramework.Manager
{
    [Serializable]
    public class EnemyWeightEntry
    {
        [Tooltip("敌人类型")]
        public EnemyTypeEnum EnemyType;

        [Tooltip("初始权重")]
        [Range(0f, 10f)]
        public float InitialWeight = 1f;

        [Tooltip("最大权重")]
        [Range(0f, 10f)]
        public float MaxWeight = 1f;
    }

    [CreateAssetMenu(fileName = "EnemySpawnConfig", menuName = "SOData/Enemy Spawn Config", order = 1)]
    public class EnemySpawnConfigSO : ScriptableObject
    {
        [Header("时间")]
        [Tooltip("每个生成刻的持续时间（秒）")]
        [Min(1f)]
        public float TickDuration = 30f;

        [Tooltip("到达最大配置所需的总时间（秒）")]
        [Min(1f)]
        public float MaxTime = 600f;

        [Header("数量")]
        [Tooltip("初始每个tick生成的敌人数量")]
        [Min(0)]
        public int InitialEnemyCount = 2;

        [Tooltip("最大每个tick生成的敌人数量")]
        [Min(0)]
        public int MaxEnemyCount = 10;

        [Header("权重")]
        [Tooltip("敌人权重列表，每项定义一种敌人类型的初始和最大权重")]
        public List<EnemyWeightEntry> Weights = new List<EnemyWeightEntry>();

        [Header("曲线")]
        [Tooltip("从初始到最大的插值曲线，X: 时间比例(0~1)，Y: 插值比例(0~1)")]
        public AnimationCurve ProgressionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    }
}
