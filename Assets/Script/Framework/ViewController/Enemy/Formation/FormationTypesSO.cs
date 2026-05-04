using System;
using System.Collections.Generic;
using QFramework.Enum;
using UnityEngine;

namespace QFramework.ViewController.Enemy.Formation
{
    /// <summary>队形敌人类型配置</summary>
    [Serializable]
    public class EnemyCount
    {
        public EnemyTypeEnum EnemyType = EnemyTypeEnum.Worker;  // 敌人类型
        public int Count = 1;  // 数量
    }

    /// <summary>
    /// 队形配置 SO，仅存储 Types 列表。
    /// 在 Project 窗口右键 Create → SOData/队形配置 创建。
    /// </summary>
    [CreateAssetMenu(fileName = "FormationTypes", menuName = "SOData/队形配置", order = 0)]
    public class FormationTypesSO : ScriptableObject
    {
        public float RingRadius = 3f;  // 最内环半径
        public float RingSpacing = 2f;  // 每层环半径递增步长
        public List<EnemyCount> Types = new List<EnemyCount>();  // 敌人种类与数量列表
    }
}
