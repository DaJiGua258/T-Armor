using QFramework.System;

namespace QFramework.Command
{
    public class LevelCommand
    {
        public class Add : AbstractCommand
        {
            private ILevelSystem _levelSystem => this.GetSystem<ILevelSystem>();
            protected override void OnExecute()
            {
                _levelSystem.AddLoadLevel();
            }
        }
    }
}