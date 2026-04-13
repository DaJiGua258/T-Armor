using QFramework.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class MainMenuPanel : AbstractBasePanel
    {
        private Button _newGameBtn;
        private Button _continueBtn;
        private Button _loadGameBtn;
        private Button _settingsBtn;
        private Button _quitBtn;

        void Awake()
        {
            _newGameBtn = transform.Find("NewGameBtn").GetComponent<Button>();
            _continueBtn = transform.Find("ContinueBtn").GetComponent<Button>();
            _loadGameBtn = transform.Find("LoadGameBtn").GetComponent<Button>();
            _settingsBtn = transform.Find("SettingsBtn").GetComponent<Button>();
            _quitBtn = transform.Find("QuitBtn").GetComponent<Button>();
        }

        void Start()
        {
            _newGameBtn.onClick.AddListener(OnNewGameClick);
            _continueBtn.onClick.AddListener(OnContinueClick);
            _loadGameBtn.onClick.AddListener(OnLoadGameClick);
            _settingsBtn.onClick.AddListener(OnSettingsClick);
            _quitBtn.onClick.AddListener(OnQuitClick);
        }



        public void OnNewGameClick()
        {
            MainUIManager.Instance.EnterLevelSelect();
        }

        public void OnContinueClick()
        {
            
        }

        public void OnLoadGameClick()
        {

        }

        public void OnSettingsClick()
        {

        }

        public void OnQuitClick()
        {
            Debug.Log("OnQuitClick");
            Application.Quit();
        }
    }
}