using QFramework.Model;
using QFramework.System;

namespace QFramework.Command
{
    public class MissionCommand
    {
        public class Add : AbstractCommand
        {
            private IMissionSystem _missionSystem => this.GetSystem<IMissionSystem>();
            private int _missionIndex;
            private int _value;

            public Add(int missionIndex, int value)
            {
                _missionIndex = missionIndex;
                this._value = value;
            }

            

            protected override void OnExecute()
            {
                if(_missionIndex < 0 || _missionIndex >= _missionSystem.Missions.Count)
                {
                    UnityEngine.Debug.LogWarning("任务索引异常，无法添加进度");
                    return;
                }

                var mission = _missionSystem.Missions[_missionIndex];
                _missionSystem.AddProgress(mission, _value);
            }
        }

        public class SetState : AbstractCommand
        {
            private IMissionSystem _missionSystem => this.GetSystem<IMissionSystem>();
            private int _missionIndex;
            private MissionState _state;

            public SetState(int missionIndex, MissionState state)
            {
                _missionIndex = missionIndex;
                _state = state;
            }

            protected override void OnExecute()
            {
                if(_missionIndex < 0 || _missionIndex >= _missionSystem.Missions.Count)
                {
                    UnityEngine.Debug.LogWarning("任务索引异常，无法设置任务状态");
                    return;
                }

                _missionSystem.Missions[_missionIndex].MissionState.Value = _state;
            }
        }
    }
}