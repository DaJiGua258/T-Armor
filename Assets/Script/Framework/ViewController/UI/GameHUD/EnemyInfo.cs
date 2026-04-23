using QFramework.Event;
using Unity.VisualScripting;
using UnityEngine;

namespace QFramework.ViewController.UI
{
    public class EnemyInfo : AbstractBasePanel
    {
        [SerializeField] private int _curEnemyId = -1;
        [SerializeField] private RectTransform _bar;
        [SerializeField] private RectTransform _enemyRect;
        [SerializeField] private RectTransform _aimRect;
        [SerializeField] private InfoItemSlider enemyInfo;

        void Start()
        {
            TypeEventSystem.Global.Register<WeaponInfoEvent.UpdateEnemyInfo>(e => UpdateEnemyInfo());
        }
        

        void Update()
        {
            UpdateSize();
        }

        private void UpdateSize()
        {
            float x = _aimRect.sizeDelta.x;
            enemyInfo.Img.rectTransform.sizeDelta = new Vector2(x, enemyInfo.Img.rectTransform.sizeDelta.y);
        }

        private void UpdateEnemyInfo()
        {   
            if(_curEnemyId == -1) return;
            var enemyData = EnemyInstanceSystem.GetData(_curEnemyId);
            enemyInfo.Img.fillAmount = (float)enemyData.CurrentHealth.Value / (float)enemyData.MaxHealth.Value;
        }

        public void SetEnemyId(int enemyId)
        {
            _curEnemyId = enemyId;
            if(enemyId == -1)
            {
                _bar.gameObject.SetActive(false);
            }
            else
            {
                _bar.gameObject.SetActive(true);
                UpdateEnemyInfo();
            }
        }
    }
}