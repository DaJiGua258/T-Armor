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

        private float _timerEndTime;
        private float _timerDuration;
        private Vector3 _timerWorldPos;
        private bool _timerActive;

        public override void OnInit()
        {
            base.OnInit();
            TypeEventSystem.Global.Register<WaveSpawnAlertEvent>(OnWaveSpawnAlert)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
            TypeEventSystem.Global.Register<InvasionTimerEvent>(OnInvasionTimer)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
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
