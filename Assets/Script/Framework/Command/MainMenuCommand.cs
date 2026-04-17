using QFramework.System;
using UnityEngine;

namespace QFramework.Command
{
    public class MainMenuCommand
    {
        public class SelectLevel : AbstractCommand
        {
            private PlanetNodeMapData _mapData;
            private ILevelSystem _levelSystem => this.GetSystem<ILevelSystem>();
            public SelectLevel(PlanetNodeMapData mapData)
            {
                _mapData = mapData;
            }

            protected override void OnExecute()
            {
                _levelSystem.InitLevelEnv(_mapData);
            }
        }
    }
}