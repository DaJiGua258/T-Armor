using QFramework.Enum;
using QFramework.System;

namespace QFramework.Command
{
    public class WeaponCommand
    {

        /// <summary>
        /// 初始化武器
        /// </summary>
        public class Init : AbstractCommand
        {
            private IPlayerSystem _playerSystem => this.GetSystem<IPlayerSystem>();

            protected override void OnExecute()
            {
                _playerSystem.InitPlayerWeapon();
                // 初始化武器数据
            }
        }
        
        


    }
}
