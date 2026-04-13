using QFramework.Manager;
using UnityEngine;

namespace QFramework.ViewController.UI
{
    public class LevelSelectPanel : AbstractBasePanel
    {

        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                MainUIManager.Instance.ResetCamera();
                MainUIManager.Instance.EnterMainMenu();
            }
        }


        public override void OnShow()
        {
            base.OnShow();
            MainUIManager.Instance.Camera.GetComponent<PlanetOrbitCamera>().enabled = true;
            MainUIManager.Instance.PlanetNodeList.gameObject.SetActive(true);
        }

        public override void OnHide()
        {
            base.OnHide();
            MainUIManager.Instance.Camera.GetComponent<PlanetOrbitCamera>().enabled = false;
            MainUIManager.Instance.PlanetNodeList.gameObject.SetActive(false);
        }
    }
}