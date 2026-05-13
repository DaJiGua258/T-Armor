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

        private Text[] _btnTexts;
        private Image[] _btnImages;
        private Sprite _defaultSprite;

        private static readonly Color s_colorBlack = Color.black;
        private static readonly Color s_colorWhite = Color.white;

        public override void OnInit()
        {
            _btnTexts = new[]
            {
                _continueBtn.transform.Find("Txt").GetComponent<Text>(),
                _settingBtn.transform.Find("Txt").GetComponent<Text>(),
                _quitBtn.transform.Find("Txt").GetComponent<Text>(),
            };

            _btnImages = new[]
            {
                _continueBtn.GetComponent<Image>(),
                _settingBtn.GetComponent<Image>(),
                _quitBtn.GetComponent<Image>(),
            };

            _defaultSprite = _btnImages[0].sprite;

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

            AddHoverHandler(_continueBtn, 0);
            AddHoverHandler(_settingBtn, 1);
            AddHoverHandler(_quitBtn, 2);
        }

        private void AddHoverHandler(Button btn, int index)
        {
            var trigger = btn.gameObject.AddComponent<EventTrigger>();

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => SetHighlight(index, true));
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => SetHighlight(index, false));
            trigger.triggers.Add(exit);
        }

        private void SetHighlight(int index, bool highlighted)
        {
            _btnTexts[index].color = highlighted ? s_colorBlack : s_colorWhite;
            _btnImages[index].sprite = highlighted ? null : _defaultSprite;
        }
    }
}
