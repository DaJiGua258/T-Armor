using System.Collections.Generic;
using UnityEngine;

namespace QFramework.Event
{
    public class InteractionEvent
    {
        // 所有在范围内的目标屏幕位置，InteractionPanel 读取后显示点
        public static readonly List<Vector2> DotScreenPositions = new List<Vector2>(16);

        public struct ShowDots { }
        public struct HideDots { }

        public struct ShowPrompt
        {
            public string ActionText;
            public string NameText;
            public Vector2 ScreenPosition;  // 提示文字显示的屏幕坐标（跟随鼠标）
        }

        public struct HidePrompt { }
    }
}
