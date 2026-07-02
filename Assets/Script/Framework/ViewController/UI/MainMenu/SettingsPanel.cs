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
        [SerializeField] private Toggle _damageNumberToggle;
        [SerializeField] private Image _damageNumberIcon;

        private const string KEY_MASTER = "MasterVolume";
        private const string KEY_ENV = "EnvVolume";
        private const string KEY_SFX = "SFXVolume";
        private const string KEY_MUTE = "IsMuted";
        private const string KEY_DAMAGE_NUM = "ShowDamageNumbers";

        public override void OnInit()
        {
            LoadSettings();

            _masterSlider.onValueChanged.AddListener(val => AudioManager.Instance.SetMasterVolume(val));
            _envSlider.onValueChanged.AddListener(val => AudioManager.Instance.SetEnvVolume(val));
            _sfxSlider.onValueChanged.AddListener(val => AudioManager.Instance.SetSFXVolume(val));

            _muteToggle.onValueChanged.AddListener(OnMuteToggleChanged);

            if (_damageNumberToggle != null)
                _damageNumberToggle.onValueChanged.AddListener(OnDamageNumberToggleChanged);
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

        private void OnDamageNumberToggleChanged(bool isOn)
        {
            DamageNumberManager.ShowDamageNumbers = isOn;
            if (_damageNumberIcon != null)
                _damageNumberIcon.enabled = isOn;
        }

        private void LoadSettings()
        {
            _masterSlider.value = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
            _envSlider.value = PlayerPrefs.GetFloat(KEY_ENV, 1f);
            _sfxSlider.value = PlayerPrefs.GetFloat(KEY_SFX, 1f);
            _muteToggle.isOn = PlayerPrefs.GetInt(KEY_MUTE, 0) == 1;

            if (_muteIcon != null)
                _muteIcon.enabled = _muteToggle.isOn;

            bool showDmg = PlayerPrefs.GetInt(KEY_DAMAGE_NUM, 1) == 1;
            DamageNumberManager.ShowDamageNumbers = showDmg;
            if (_damageNumberToggle != null)
                _damageNumberToggle.isOn = showDmg;
            if (_damageNumberIcon != null)
                _damageNumberIcon.enabled = showDmg;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(KEY_MASTER, _masterSlider.value);
            PlayerPrefs.SetFloat(KEY_ENV, _envSlider.value);
            PlayerPrefs.SetFloat(KEY_SFX, _sfxSlider.value);
            PlayerPrefs.SetInt(KEY_MUTE, _muteToggle.isOn ? 1 : 0);
            PlayerPrefs.SetInt(KEY_DAMAGE_NUM, DamageNumberManager.ShowDamageNumbers ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
