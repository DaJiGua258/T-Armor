using QFramework.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class GameOverPanel : AbstractBasePanel
    {
        [SerializeField] private Button _restartBtn;

        public override void OnInit()
        {
            base.OnInit();
            _restartBtn.onClick.AddListener(() => 
                {
                    GameManager.Instance.EnterMainScene();
                    Debug.Log("Restart");
                }
            );
        }
    }
}