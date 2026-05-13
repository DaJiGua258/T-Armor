using QFramework.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class SettingsPanel : AbstractBasePanel
    {
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _envSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Toggle _muteToggle;
        [SerializeField] private Image _muteIcon;

        private const string KEY_MASTER = "MasterVolume";
        private const string KEY_ENV = "EnvVolume";
        private const string KEY_SFX = "SFXVolume";
        private const string KEY_MUTE = "IsMuted";

        public override void OnInit()
        {
            LoadSettings();

            _masterSlider.onValueChanged.AddListener(val => AudioManager.Instance.SetMasterVolume(val));
            _envSlider.onValueChanged.AddListener(val => AudioManager.Instance.SetEnvVolume(val));
            _sfxSlider.onValueChanged.AddListener(val => AudioManager.Instance.SetSFXVolume(val));

            _muteToggle.onValueChanged.AddListener(OnMuteToggleChanged);
        }

        public override void OnShow()
        {
            base.OnShow();
            LoadSettings();
        }

        public override void OnHide()
        {
            base.OnHide();
            SaveSettings();
        }

        private void OnMuteToggleChanged(bool isOn)
        {
            AudioManager.Instance.SetMute(isOn);
            if (_muteIcon != null)
                _muteIcon.enabled = isOn;
        }

        private void LoadSettings()
        {
            _masterSlider.value = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
            _envSlider.value = PlayerPrefs.GetFloat(KEY_ENV, 1f);
            _sfxSlider.value = PlayerPrefs.GetFloat(KEY_SFX, 1f);
            _muteToggle.isOn = PlayerPrefs.GetInt(KEY_MUTE, 0) == 1;

            if (_muteIcon != null)
                _muteIcon.enabled = _muteToggle.isOn;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(KEY_MASTER, _masterSlider.value);
            PlayerPrefs.SetFloat(KEY_ENV, _envSlider.value);
            PlayerPrefs.SetFloat(KEY_SFX, _sfxSlider.value);
            PlayerPrefs.SetInt(KEY_MUTE, _muteToggle.isOn ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
