using QFramework.Event;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class InteractionPanel : AbstractBasePanel
    {
        [SerializeField] private GameObject _promptRoot;
        [SerializeField] private Text _actionText;
        [SerializeField] private Text _nameText;

        public override void OnInit()
        {
            gameObject.SetActive(true);
            _promptRoot.SetActive(false);

            TypeEventSystem.Global.Register<InteractionEvent.ShowPrompt>(
                OnShowPrompt
            ).UnRegisterWhenGameObjectDestroyed(gameObject);

            TypeEventSystem.Global.Register<InteractionEvent.HidePrompt>(
                OnHidePrompt
            ).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void OnShowPrompt(InteractionEvent.ShowPrompt e)
        {
            _actionText.text = e.ActionText;
            _nameText.text = e.NameText;
            _promptRoot.SetActive(true);
        }

        private void OnHidePrompt(InteractionEvent.HidePrompt e)
        {
            _promptRoot.SetActive(false);
        }
    }
}
