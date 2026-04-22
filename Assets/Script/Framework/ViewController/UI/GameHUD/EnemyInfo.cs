using UnityEngine;

namespace QFramework.ViewController.UI
{
    public class EnemyInfo : AbstractBasePanel
    {
        [SerializeField] private RectTransform _enemyRect;
        [SerializeField] private RectTransform _aimRect;
        [SerializeField] private InfoItemSlider enemyInfo;

        void Update()
        {
            UpdateSize();
        }

        private void UpdateSize()
        {
            float x = _aimRect.sizeDelta.x;
            enemyInfo.Img.rectTransform.sizeDelta = new Vector2(x, enemyInfo.Img.rectTransform.sizeDelta.y);
        }
    }
}