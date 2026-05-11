using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI.WeaponConfig
{
    public class ScrollWheelToHorizontal : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private float _scrollSpeed = 0.1f;

        private void Update()
        {
            float delta = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(delta) > 0.01f)
            {
                _scrollRect.horizontalNormalizedPosition += delta * _scrollSpeed;
            }
        }
    }
}
