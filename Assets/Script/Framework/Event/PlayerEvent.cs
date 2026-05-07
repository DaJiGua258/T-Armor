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
            public bool HasTarget;
        }

        /// <summary> 瞄准模式切换事件 /// </summary>
        public struct SwitchAimingMode
        {
            public AimingModeEnum Mode;
        }

        /// <summary> 引导激光显示 /// </summary>
        public struct GuidanceLaserShow { }

        /// <summary> 引导激光隐藏 /// </summary>
        public struct GuidanceLaserHide { }

        /// <summary> 引导激光引导中（绿色） /// </summary>
        public struct GuidanceLaserSetChanneling { }
    }
}