using System.Collections;
using System.Collections.Generic;
using QFramework;
using QFramework.Event;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class GameHUDPanel : AbstractBasePanel
    {
        [SerializeField] private GameObject _alertNode;

        [Header("入侵倒计时")]
        [SerializeField] private RectTransform _timerRoot;
        [SerializeField] private Image _invasionFillImage;
        [SerializeField] private Vector2 _screenOffset;

        [Header("受击闪红")]
        [SerializeField] private Color _flashColor = Color.red;
        [SerializeField] private float _flashDuration = 0.15f;

        private float _timerEndTime;
        private float _timerDuration;
        private Vector3 _timerWorldPos;
        private bool _timerActive;

        private List<Image> _hudImages = new();
        private List<Color> _originalColors = new();
        private Coroutine _flashCoroutine;

        public override void OnInit()
        {
            base.OnInit();

            CacheOriginalColors();

            TypeEventSystem.Global.Register<WaveSpawnAlertEvent>(OnWaveSpawnAlert)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
            TypeEventSystem.Global.Register<InvasionTimerEvent>(OnInvasionTimer)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
            TypeEventSystem.Global.Register<StatsEvent.OnDamageTaken>(OnDamageTaken)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void CacheOriginalColors()
        {
            _hudImages.Clear();
            _originalColors.Clear();
            GetComponentsInChildren(_hudImages);
            foreach (var img in _hudImages)
                _originalColors.Add(img.color);
        }

        private void OnDamageTaken(StatsEvent.OnDamageTaken e)
        {
            if (_hudImages.Count == 0) return;
            if (!gameObject.activeInHierarchy) return;

            if (_flashCoroutine != null)
                StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            foreach (var img in _hudImages)
                img.color = _flashColor;

            float timer = _flashDuration;
            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                float t = Mathf.Clamp01(timer / _flashDuration);
                for (int i = 0; i < _hudImages.Count; i++)
                    _hudImages[i].color = Color.Lerp(_flashColor, _originalColors[i], 1f - t);
                yield return null;
            }

            for (int i = 0; i < _hudImages.Count; i++)
                _hudImages[i].color = _originalColors[i];
            _flashCoroutine = null;
        }

        private void Update()
        {
            if (!_timerActive) return;

            float remaining = _timerEndTime - Time.time;
            if (remaining <= 0f)
            {
                StopTimer();
                return;
            }

            _timerRoot.position = Camera.main.WorldToScreenPoint(_timerWorldPos) + (Vector3)_screenOffset;
            _invasionFillImage.fillAmount = remaining / _timerDuration;
        }

        private void OnWaveSpawnAlert(WaveSpawnAlertEvent e)
        {
            if (_alertNode != null) _alertNode.SetActive(e.Active);
        }

        private void OnInvasionTimer(InvasionTimerEvent e)
        {
            if (e.Active)
            {
                _timerActive = true;
                _timerDuration = e.Duration;
                _timerEndTime = Time.time + e.Duration;
                _timerWorldPos = e.TerminalWorldPos;
                _timerRoot.gameObject.SetActive(true);
                _invasionFillImage.fillAmount = 1f;
            }
            else
            {
                StopTimer();
            }
        }

        private void StopTimer()
        {
            _timerActive = false;
            _timerRoot.gameObject.SetActive(false);
        }
    }
}
