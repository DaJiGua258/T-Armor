using QFramework.Manager;
using QFramework.Model;
using QFramework.System;
using QFramework.Utility;
using UnityEngine;
using UnityEngine.EventSystems;
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
            Transform content = transform.Find("Content");
            _newGameBtn = content.Find("NewGameBtn").GetComponent<Button>();
            _continueBtn = content.Find("ContinueBtn").GetComponent<Button>();
            _loadGameBtn = content.Find("LoadGameBtn").GetComponent<Button>();
            _settingsBtn = content.Find("SettingsBtn").GetComponent<Button>();
            _quitBtn = content.Find("QuitBtn").GetComponent<Button>();
        }

        void Start()
        {
            // 检查存档状态，控制 Continue 按钮
            var storage = this.GetUtility<IStorageUtility>();
            var saveData = storage.LoadData<GameSaveData>("GameSaveData");
            bool hasSave = saveData != null && saveData.CompletedLevels.Count > 0;
            _continueBtn.interactable = hasSave;
            var continueTxt = _continueBtn.transform.Find("Txt").GetComponent<Text>();
            continueTxt.color = hasSave ? Color.white : Color.gray;

            // 统一设置按钮高亮（跳过不可用的按钮）
            var buttons = new[] { _newGameBtn, _continueBtn, _loadGameBtn, _settingsBtn, _quitBtn };
            foreach (var btn in buttons)
            {
                var highlight = btn.gameObject.AddComponent<UIHighlight>();
                highlight.Setup(
                    btn.GetComponent<Image>(),
                    btn.transform.Find("Txt").GetComponent<Text>()
                );
                AddHoverHandler(btn, highlight);
            }

            _newGameBtn.onClick.AddListener(OnNewGameClick);
            _continueBtn.onClick.AddListener(OnContinueClick);
            _loadGameBtn.onClick.AddListener(OnLoadGameClick);
            _settingsBtn.onClick.AddListener(OnSettingsClick);
            _quitBtn.onClick.AddListener(OnQuitClick);
        }

        private void AddHoverHandler(Button btn, UIHighlight highlight)
        {
            var trigger = btn.gameObject.AddComponent<EventTrigger>();

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ =>
            {
                if (btn.interactable) highlight.SetHighlight(true);
            });
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => highlight.SetHighlight(false));
            trigger.triggers.Add(exit);
        }



        public void OnNewGameClick()
        {
            // 清除旧存档
            var storage = this.GetUtility<IStorageUtility>();
            storage.DeleteData("GameSaveData");

            // 重置关卡缓存
            var levelSystem = this.GetSystem<ILevelSystem>();
            levelSystem.LevelDataCache.Clear();

            MainUIManager.Instance.EnterLevelSelect();
        }

        public void OnContinueClick()
        {
            if (!_continueBtn.interactable) return;

            var levelSystem = this.GetSystem<ILevelSystem>();
            if (levelSystem.LevelDataCache.Count > 0)
            {
                MainUIManager.Instance.EnterLevelSelect();
            }
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