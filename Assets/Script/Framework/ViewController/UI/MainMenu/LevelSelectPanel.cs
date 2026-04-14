using QFramework.Manager;
using QFramework.Utility;
using Unity.VisualScripting;
using UnityEngine;

namespace QFramework.ViewController.UI
{
    public class LevelSelectPanel : AbstractBasePanel
    {

        void Update()
        {
            if(MainUIManager.Instance.CurrentPanel == UIMainPanelType.LevelSelectPanel && InputUtility.GetESCInput())
            {
                MainUIManager.Instance.ResetCamera();
                MainUIManager.Instance.EnterMainMenu();
                MainUIManager.Instance.LockCamera();
            }

            if((MainUIManager.Instance.CurrentPanel == UIMainPanelType.LevelDetailPanel || 
               MainUIManager.Instance.CurrentPanel == UIMainPanelType.EquipmentConfigPanel)
                && InputUtility.GetESCInput())
            {
                MainUIManager.Instance.EnterLevelSelect();
                MainUIManager.Instance.UnlockCamera();
            }
        }


        public override void OnShow()
        {
            base.OnShow();
            MainUIManager.Instance.UnlockCamera();
            MainUIManager.Instance.PlanetNodeList.gameObject.SetActive(true);
        }

        public override void OnHide()
        {
            base.OnHide();
            MainUIManager.Instance.LockCamera();
            MainUIManager.Instance.PlanetNodeList.gameObject.SetActive(false);
        }
    }
}