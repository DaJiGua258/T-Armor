using QFramework.Command;
using QFramework.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI  
{
    public class LevelDetailPanel : AbstractBasePanel
    {
        private MapInfoDetailContainer _mapInfoDetailContainer;
        private Button _deployBtn;
        
        public override void OnInit()
        {
            Canvas.ForceUpdateCanvases();

            _mapInfoDetailContainer = transform.Find
                ("MapInfoContainer/MapInfoDetailContainer").GetComponent<MapInfoDetailContainer>();

            _mapInfoDetailContainer.InitMapInfoDetail();

            _deployBtn = transform.Find("DeployBtn/Container/Img").GetComponent<Button>();
            _deployBtn.onClick.AddListener(() =>
            {
                this.SendCommand<LevelCommand.Add>();
                GameManager.Instance.EnterGameScene();
            });
        }
    }
}
