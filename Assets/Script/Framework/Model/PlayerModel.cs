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
            MaxHealth.Value = 999;
            CurrentHealth.Value = 999;
            Speed.Value = 3;
            MaxFuel.Value = 100;
            CurrentFuel.Value = 100;

            FuelRecovery = 8;
            DashCost = 10;
            SprintCost = 5;
            SprintSmooth = 3;
        }
        
        
    }
}