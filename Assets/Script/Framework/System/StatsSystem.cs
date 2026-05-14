using System.Collections.Generic;
using QFramework.Enum;
using QFramework.Event;
using QFramework.Utility;
using UnityEngine;

namespace QFramework.System
{
    public interface IStatsSystem : ISystem
    {
        int TotalKills { get; }
        int TotalDamageDealt { get; }
        int TotalDamageTaken { get; }
        int TotalShotsFired { get; }
        int TotalDeaths { get; }
        int TotalMissionsCompleted { get; }
        float GameTimeSeconds { get; }
        float GetAccuracy();
        int GetKillsByType(EnemyTypeEnum type);
        Dictionary<EnemyTypeEnum, int> KillsPerEnemyType { get; }
    }

    public class StatsSystem : AbstractSystem, IStatsSystem
    {
        // ── 统计字段 ─────────────────────────────────────
        private int _totalKills;
        private int _totalDamageDealt;
        private int _totalDamageTaken;
        private int _totalShotsFired;
        private int _totalDeaths;
        private int _totalMissionsCompleted;
        private int _totalHits;
        private float _gameStartTime;
        private float _frozenGameTime;
        private bool _gameEnded;
        private Dictionary<EnemyTypeEnum, int> _killsPerEnemyType = new();

        // ── 公开属性 ─────────────────────────────────────
        public int TotalKills => _totalKills;
        public int TotalDamageDealt => _totalDamageDealt;
        public int TotalDamageTaken => _totalDamageTaken;
        public int TotalShotsFired => _totalShotsFired;
        public int TotalDeaths => _totalDeaths;
        public int TotalMissionsCompleted => _totalMissionsCompleted;
        public Dictionary<EnemyTypeEnum, int> KillsPerEnemyType => _killsPerEnemyType;

        public float GameTimeSeconds
        {
            get
            {
                if (_gameEnded) return _frozenGameTime;
                return Time.time - _gameStartTime;
            }
        }

        public float GetAccuracy()
        {
            if (_totalShotsFired <= 0) return 0f;
            return (float)_totalHits / _totalShotsFired;
        }

        public int GetKillsByType(EnemyTypeEnum type)
        {
            return _killsPerEnemyType.TryGetValue(type, out int count) ? count : 0;
        }

        protected override void OnInit()
        {
            _gameStartTime = Time.time;

            TypeEventSystem.Global.Register<StatsEvent.OnEnemyKilled>(OnEnemyKilled);
            TypeEventSystem.Global.Register<StatsEvent.OnDamageDealt>(OnDamageDealt);
            TypeEventSystem.Global.Register<StatsEvent.OnDamageTaken>(OnDamageTaken);
            TypeEventSystem.Global.Register<StatsEvent.OnShotFired>(OnShotFired);
            TypeEventSystem.Global.Register<StatsEvent.OnPlayerDeath>(OnPlayerDeath);
            TypeEventSystem.Global.Register<StatsEvent.OnMissionCompleted>(OnMissionCompleted);
        }

        // ── 事件处理 ─────────────────────────────────────
        private void OnEnemyKilled(StatsEvent.OnEnemyKilled e)
        {
            _totalKills++;

            if (_killsPerEnemyType.ContainsKey(e.Type))
                _killsPerEnemyType[e.Type]++;
            else
                _killsPerEnemyType[e.Type] = 1;
        }

        private void OnDamageDealt(StatsEvent.OnDamageDealt e)
        {
            _totalDamageDealt += Mathf.RoundToInt(e.Damage);
            _totalHits++;
        }

        private void OnDamageTaken(StatsEvent.OnDamageTaken e)
        {
            _totalDamageTaken += Mathf.RoundToInt(e.Damage);
        }

        private void OnShotFired(StatsEvent.OnShotFired e)
        {
            _totalShotsFired++;
        }

        private void OnPlayerDeath(StatsEvent.OnPlayerDeath e)
        {
            _totalDeaths++;
            _frozenGameTime = Time.time - _gameStartTime;
            _gameEnded = true;
        }

        private void OnMissionCompleted(StatsEvent.OnMissionCompleted e)
        {
            _totalMissionsCompleted++;
        }
    }
}
