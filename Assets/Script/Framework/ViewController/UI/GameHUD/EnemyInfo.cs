using QFramework.Event;
using Unity.VisualScripting;
using UnityEngine;

namespace QFramework.ViewController.UI
{
    public class EnemyInfo : BaseUIComponent
    {
        [SerializeField] private int _curEnemyId = -1;
        [SerializeField] private RectTransform _bar;
        [SerializeField] private RectTransform _enemyRect;
        [SerializeField] private RectTransform _aimRect;
        [SerializeField] private InfoItemSlider enemyInfo;
        [SerializeField] private CanvasGroup _canvasGroup;

        void Start()
        {
            TypeEventSystem.Global.Register<WeaponInfoEvent.UpdateEnemyInfo>(e => UpdateEnemyInfo())
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            TypeEventSystem.Global.Register<PlayerEvent.SwitchAimingMode>(
                e => _canvasGroup.alpha = e.Mode == AimingModeEnum.Combat ? 1f : 0f
            ).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
        

        void Update()
        {
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