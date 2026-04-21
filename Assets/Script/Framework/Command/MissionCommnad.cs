using QFramework.Model;
using QFramework.System;

namespace QFramework.Command
{
    public class MissionCommand
    {
        public class AddPri : AbstractCommand
        {
            private IMissionSystem _missionSystem => this.GetSystem<IMissionSystem>();
            private int _value;

            public AddPri(int value)
            {
                this._value = value;
            }

            protected override void OnExecute()
            {
                if(_missionSystem.PrimaryMission.MissionState.Value == MissionState.NotStarted)
                {
                    UnityEngine.Debug.LogWarning("Primary mission is not not started, cannot add progress");
                    return;
                }

                var mission = _missionSystem.PrimaryMission;

                _missionSystem.AddProgress(mission, _value);
            }
        }

        public class AddPre : AbstractCommand
        {
            private IMissionSystem _missionSystem => this.GetSystem<IMissionSystem>();
            private int _value;
            private int _index;

            public AddPre(int index, int value)
            {
                this._value = value;
                this._index = index;
            }

            protected override void OnExecute()
            {
                // var mission = _missionSystem.GetPreByIndex(_index);
                var mission = _missionSystem.PrerequiredMissions[_index];   
                if(mission != null)
                {
                    _missionSystem.AddProgress(mission, _value);
                }
            }
        }
    }
}