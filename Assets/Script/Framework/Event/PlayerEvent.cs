using QFramework.ViewController.UI;
using UnityEngine;

namespace QFramework.Event
{
    public class PlayerEvent
    {
        /// <summary> 玩家目标位置事件 /// </summary>
        public struct UpdateTarget
        {
            public Vector2 Target;
        }

        /// <summary> 瞄准模式切换事件 /// </summary>
        public struct SwitchAimingMode
        {
            public AimingModeEnum Mode;
        }
    }
}