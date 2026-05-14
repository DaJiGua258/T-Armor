using QFramework.Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class PausePanel : AbstractBasePanel
    {
        [SerializeField] private Button _continueBtn;
        [SerializeField] private Button _settingBtn;
        [SerializeField] private Button _quitBtn;

        public override void OnInit()
        {
            var buttons = new[] { _continueBtn, _settingBtn, _quitBtn };

            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];
                var highlight = btn.gameObject.AddComponent<UIHighlight>();
                highlight.Setup(
                    btn.GetComponent<Image>(),
                    btn.transform.Find("Txt").GetComponent<Text>()
                );
                AddHoverHandler(btn, highlight);
            }

            _continueBtn.onClick.AddListener(() =>
            {
                UIGameManager.Instance.HidePanel(UIGamePanelType.PausePanel);
                GameManager.Instance.SetRuntimeState(RuntimeGameState.Playing);
            });

            _settingBtn.onClick.AddListener(() =>
            {
                UIGameManager.Instance.HidePanel(UIGamePanelType.PausePanel);
                UIGameManager.Instance.ShowPanel(UIGamePanelType.SettingsPanel);
            });

            _quitBtn.onClick.AddListener(() =>
            {
                GameManager.Instance.EnterMainScene();
            });
        }

        private void AddHoverHandler(Button btn, UIHighlight highlight)
        {
            var trigger = btn.gameObject.AddComponent<EventTrigger>();

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => highlight.SetHighlight(true));
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => highlight.SetHighlight(false));
            trigger.triggers.Add(exit);
        }
    }
}
