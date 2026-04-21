using UnityEngine;

namespace QFramework.Event
{
    public class PlayerEvent
    {
        /// <summary>
        /// 注册事件：更新目标世界空间位置
        /// </summary>
        public struct UpdateTarget
        {
            public Vector2 Target;
        }
    }
}