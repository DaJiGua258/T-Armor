namespace QFramework.Model
{
    public interface IPlayerModel : IModel
    {
        BindableProperty<int> MaxHealth { get; }
        BindableProperty<int> CurrentHealth { get; }
        BindableProperty<int> Speed { get; }
    }
    public class PlayerModel : AbstractModel, IPlayerModel
    {
        public BindableProperty<int> MaxHealth { get; } = new BindableProperty<int>();
        public BindableProperty<int> CurrentHealth { get; } = new BindableProperty<int>();
        public BindableProperty<int> Speed { get; } = new BindableProperty<int>();

        protected override void OnInit()
        {
            MaxHealth.Value = 100;
            CurrentHealth.Value = 100;
            Speed.Value = 3;
        }
        
        
    }
}