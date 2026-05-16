using QFramework.Event;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class InteractionPanel : AbstractBasePanel
    {
        [SerializeField] private GameObject _tipRoot;
        [SerializeField] private Text _actionText;
        [SerializeField] private Text _nameText;

        [Header("提示文字偏移")]
        [SerializeField] private Vector2 _tipOffset = new Vector2(10, -20);

        [Header("点指示器")]
        [SerializeField] private RectTransform _dotPrefab;
        [SerializeField] private int _maxDots = 16;

        private RectTransform[] _dots;
        private int _dotCount;

        public override void OnInit()
        {
            gameObject.SetActive(true);
            _tipRoot.SetActive(false);

            _dots = new RectTransform[_maxDots];
            for (int i = 0; i < _maxDots; i++)
            {
                _dots[i] = Instantiate(_dotPrefab, transform);
                _dots[i].gameObject.SetActive(false);
            }

            TypeEventSystem.Global.Register<InteractionEvent.ShowDots>(
                OnShowDots
            ).UnRegisterWhenGameObjectDestroyed(gameObject);

            TypeEventSystem.Global.Register<InteractionEvent.HideDots>(
                OnHideDots
            ).UnRegisterWhenGameObjectDestroyed(gameObject);

            TypeEventSystem.Global.Register<InteractionEvent.ShowPrompt>(
                OnShowPrompt
            ).UnRegisterWhenGameObjectDestroyed(gameObject);

            TypeEventSystem.Global.Register<InteractionEvent.HidePrompt>(
                OnHidePrompt
            ).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void Update()
        {
            if (_tipRoot.activeSelf)
                _tipRoot.transform.position = (Vector2)Input.mousePosition + _tipOffset;
        }

        private void OnShowDots(InteractionEvent.ShowDots e)
        {
            var positions = InteractionEvent.DotScreenPositions;
            int count = Mathf.Min(positions.Count, _maxDots);

            for (int i = 0; i < count; i++)
            {
                _dots[i].position = positions[i];
                _dots[i].gameObject.SetActive(true);
            }

            for (int i = count; i < _dotCount; i++)
                _dots[i].gameObject.SetActive(false);

            _dotCount = count;
        }

        private void OnHideDots(InteractionEvent.HideDots e)
        {
            for (int i = 0; i < _dotCount; i++)
                _dots[i].gameObject.SetActive(false);
            _dotCount = 0;
        }

        private void OnShowPrompt(InteractionEvent.ShowPrompt e)
        {
            _actionText.text = e.ActionText;
            _nameText.text = e.NameText;
            _tipRoot.SetActive(true);
        }

        private void OnHidePrompt(InteractionEvent.HidePrompt e)
        {
            _tipRoot.SetActive(false);
        }
    }
}
