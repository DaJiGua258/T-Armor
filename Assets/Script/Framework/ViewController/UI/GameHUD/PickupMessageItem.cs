using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class PickupMessageItem : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Text _nameText;
        [SerializeField] private CanvasGroup _canvasGroup;

        private RectTransform _rectTransform;
        private Sequence _sequence;
        private int _generation;

        public bool IsAvailable { get; private set; } = true;
        public RectTransform RectTransform => _rectTransform;
        public Action<PickupMessageItem> OnAnimationComplete;

        private static readonly Vector3 OffScreenPos = new Vector3(-9999f, -9999f, 0f);

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        public void Show(Sprite icon, string name, float slideFromX, float slideDuration, float displayDuration,
            float fadeOutDuration)
        {
            _generation++;
            int capturedGen = _generation;

            IsAvailable = false;
            gameObject.SetActive(true);

            _icon.sprite = icon;
            _nameText.text = name;
            _canvasGroup.alpha = 0f;

            // 初始 X 在屏幕外
            var pos = _rectTransform.anchoredPosition;
            pos.x = slideFromX;
            _rectTransform.anchoredPosition = pos;

            // Kill 旧序列
            _sequence?.Kill();
            _sequence = DOTween.Sequence();

            // Phase 1: SlideIn + FadeIn 并行
            _sequence.Join(_rectTransform.DOAnchorPosX(0f, slideDuration).SetEase(Ease.OutBack));
            _sequence.Join(_canvasGroup.DOFade(1f, slideDuration * 0.75f));

            // Phase 2: 停留
            _sequence.AppendInterval(displayDuration);

            // Phase 3: FadeOut
            _sequence.Append(_canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));

            _sequence.OnComplete(() =>
            {
                if (capturedGen != _generation) return;
                ResetState();
                OnAnimationComplete?.Invoke(this);
            });

            _sequence.Play();
        }

        public void ResetState()
        {
            _generation++;
            _sequence?.Kill();
            _sequence = null;

            _canvasGroup.alpha = 0f;
            IsAvailable = true;
            gameObject.SetActive(false);

            if (_rectTransform != null)
                _rectTransform.anchoredPosition = OffScreenPos;
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
        }
    }
}
