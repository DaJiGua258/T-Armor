using System.Linq;
using UnityEngine;

namespace QFramework.Model
{
    public interface IPlayerModel : IModel
    {
        BindableProperty<int> MaxHealth { get; }
        BindableProperty<int> CurrentHealth { get; }
        BindableProperty<int> MaxFuel { get; }
        BindableProperty<float> CurrentFuel { get; }
        BindableProperty<int> Speed { get; }

        public float FuelRecovery { get; set; }  // 每秒恢复
        int DashCost { get; }
        int SprintCost { get; }
        int SprintSmooth { get; }
    }
    public class PlayerModel : AbstractModel, IPlayerModel
    {
        public BindableProperty<int> MaxHealth { get; } = new BindableProperty<int>();
        public BindableProperty<int> CurrentHealth { get; } = new BindableProperty<int>();
        public BindableProperty<int> MaxFuel { get; } = new BindableProperty<int>();
        public BindableProperty<float> CurrentFuel { get; } = new BindableProperty<float>();
        public BindableProperty<int> Speed { get; } = new BindableProperty<int>();

        public float FuelRecovery { get; set; }  // 每秒恢复
        public int DashCost { get; set; }  // 每次消耗
        public int SprintCost { get; set; }  // 每秒消耗

        public int SprintSmooth { get; set; }  // 冲刺平滑

        protected override void OnInit()
        {
            var configs = ConfigLoader.LoadFromJson<PlayerConfig>("Config/PlayerConfig");
            var cfg = configs.FirstOrDefault();
            if (cfg == null)
            {
                Debug.LogError("[PlayerModel] PlayerConfig 加载失败，使用默认值");
                ApplyConfig(new PlayerConfig { MaxHealth = 999, Speed = 3, MaxFuel = 100, FuelRecovery = 8, DashCost = 10, SprintCost = 5, SprintSmooth = 3 });
                return;
            }

            Debug.Log($"[PlayerModel] 从 JSON 加载玩家配置: HP={cfg.MaxHealth}, Speed={cfg.Speed}");
            ApplyConfig(cfg);
        }

        private void ApplyConfig(PlayerConfig cfg)
        {
            MaxHealth.Value = cfg.MaxHealth;
            CurrentHealth.Value = cfg.MaxHealth;
            Speed.Value = cfg.Speed;
            MaxFuel.Value = cfg.MaxFuel;
            CurrentFuel.Value = cfg.MaxFuel;
            FuelRecovery = cfg.FuelRecovery;
            DashCost = cfg.DashCost;
            SprintCost = cfg.SprintCost;
            SprintSmooth = cfg.SprintSmooth;
        }
    }
}