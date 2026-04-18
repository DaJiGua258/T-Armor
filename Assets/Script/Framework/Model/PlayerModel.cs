namespace QFramework.Model
{
    public interface IPlayerModel : IModel
    {
        BindableProperty<int> MaxHealth { get; }
        BindableProperty<int> CurrentHealth { get; }
        BindableProperty<int> MaxFuel { get; }
        BindableProperty<int> CurrentFuel { get; }
        BindableProperty<int> Speed { get; }
    }
    public class PlayerModel : AbstractModel, IPlayerModel
    {
        public BindableProperty<int> MaxHealth { get; } = new BindableProperty<int>();
        public BindableProperty<int> CurrentHealth { get; } = new BindableProperty<int>();
        public BindableProperty<int> MaxFuel { get; } = new BindableProperty<int>();
        public BindableProperty<int> CurrentFuel { get; } = new BindableProperty<int>();
        public BindableProperty<int> Speed { get; } = new BindableProperty<int>();

        protected override void OnInit()
        {
            MaxHealth.Value = 100;
            CurrentHealth.Value = 100;
            Speed.Value = 3;
            MaxFuel.Value = 100;
            CurrentFuel.Value = 100;
        }
        
        
    }
}