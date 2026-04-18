using QFramework.UtilityKit;
using UnityEngine;

namespace Framework.ViewController.UI
{
    public enum CursorType
    {
        Normal,
    }

    public class CursorManager : MonoBehaviour
    {
        [SerializeField]  private Texture2D _normalCursor;
        void Start()
        {
            SetCursor(CursorType.Normal);
        }

        public void SetCursor(CursorType cursorType)
        {
            switch (cursorType)
            {
                case CursorType.Normal:
                    Vector2 hotSpot = new Vector2(_normalCursor.width / 2, _normalCursor.height / 2);
                    Cursor.SetCursor(_normalCursor, hotSpot, CursorMode.Auto);
                    break;
            }
        }
    }
}