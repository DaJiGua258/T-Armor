using QFramework.System;

namespace QFramework.Command
{
    public class MissionCommand
    {
        public class Add : AbstractCommand
        {
            private IMissionSystem _missionSystem => this.GetSystem<IMissionSystem>();
            private int _value;

            public Add(int value)
            {
                this._value = value;
            }

            protected override void OnExecute()
            {
                var mission = _missionSystem.PrimaryMission;

                _missionSystem.AddMissionProgress(mission, _value);
            }
        }
    }
}