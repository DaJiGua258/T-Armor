using System;
using System.Collections;
using System.Collections.Generic;
using QFramework.Enum;
using QFramework.Utility;
using QFramework.UtilityKit;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QFramework.Manager
{
    public class AudioManager : MonoSingleton<AudioManager>, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        [SerializeField] private int _poolSize = 4;
        [SerializeField] private float _defaultFadeDuration = 1f;

        private Dictionary<string, AudioClip> _audioCache;
        private AudioSource _bgmSource;
        private AudioSource _envSource;
        private AudioSource[] _sfxPool;

        [SerializeField] private float _sfxPitchVariation = 0.1f;
        [SerializeField] private float _sfxVolumeVariation = 0.05f;

        private float _masterVol = 1f;
        private float _bgmVol = 1f;
        private float _envVol = 1f;
        private float _sfxVol = 1f;
        private bool _isMuted;

        public float MasterVolume => _masterVol;
        public float BGMVolume => _bgmVol;
        public float EnvVolume => _envVol;
        public float SFXVolume => _sfxVol;
        public bool IsMuted => _isMuted;

        protected override void Awake()
        {
            base.Awake();

            _audioCache = new Dictionary<string, AudioClip>();

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;

            _envSource = gameObject.AddComponent<AudioSource>();
            _envSource.loop = true;
            _envSource.playOnAwake = false;

            _poolSize = Mathf.Max(_poolSize, 1);
            _sfxPool = new AudioSource[_poolSize];
            for (int i = 0; i < _poolSize; i++)
            {
                _sfxPool[i] = gameObject.AddComponent<AudioSource>();
                _sfxPool[i].playOnAwake = false;
            }

            LoadSavedSettings();

            // 根据场景自动播放环境音
            var sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "Game")
                PlayEnv(EnvType.game);
            else
                PlayEnv(EnvType.main_menu);
        }

        // ===== BGM =====

        public void PlayBGM(BGMType type, float fadeDuration = -1f)
        {
            if (_isMuted) return;

            var clip = LoadClip("BGMType", type.ToString());
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] BGM clip not found: {type}");
                return;
            }

            if (fadeDuration < 0f) fadeDuration = _defaultFadeDuration;
            StartCoroutine(CrossFadeBGM(clip, fadeDuration));
        }

        public void StopBGM(float fadeDuration = -1f)
        {
            if (!_bgmSource.isPlaying) return;

            if (fadeDuration < 0f) fadeDuration = _defaultFadeDuration;

            if (fadeDuration > 0f)
            {
                StartCoroutine(FadeOutBGMCoroutine(fadeDuration));
            }
            else
            {
                _bgmSource.Stop();
            }
        }

        public void PauseBGM()
        {
            _bgmSource.Pause();
        }

        public void ResumeBGM()
        {
            if (_isMuted) return;
            _bgmSource.UnPause();
        }

        private IEnumerator CrossFadeBGM(AudioClip newClip, float duration)
        {
            if (_bgmSource.isPlaying && _bgmSource.clip != null)
            {
                float startVol = _bgmSource.volume;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    _bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                    yield return null;
                }
                _bgmSource.Stop();
            }

            _bgmSource.clip = newClip;
            float targetVol = GetBGMVolumeMultiplier();
            _bgmSource.volume = 0f;
            _bgmSource.Play();

            float fadeElapsed = 0f;
            while (fadeElapsed < duration)
            {
                fadeElapsed += Time.deltaTime;
                _bgmSource.volume = Mathf.Lerp(0f, targetVol, fadeElapsed / duration);
                yield return null;
            }
            _bgmSource.volume = targetVol;
        }

        private IEnumerator FadeOutBGMCoroutine(float duration)
        {
            float startVol = _bgmSource.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }
            _bgmSource.Stop();
        }

        // ===== Environment =====

        public void PlayEnv(AudioClip clip, bool loop = true)
        {
            if (_isMuted) return;
            if (clip == null) return;

            _envSource.clip = clip;
            _envSource.loop = loop;
            _envSource.volume = GetEnvVolumeMultiplier();
            _envSource.Play();
        }

        public void PlayEnv(EnvType type)
        {
            if (_isMuted) return;

            var clip = LoadClip("EnvType", type.ToString());
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] Env clip not found: {type}");
                return;
            }

            _envSource.clip = clip;
            _envSource.loop = true;
            _envSource.volume = GetEnvVolumeMultiplier();
            _envSource.Play();
        }

        public void StopEnv()
        {
            _envSource.Stop();
        }

        // ===== SFX =====

        public void PlaySFX(SFXType type)
        {
            if (_isMuted) return;

            var clip = LoadClip("SFXType", type.ToString());
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] SFX clip not found: {type}");
                return;
            }

            var source = GetAvailableSFXSource();
            ApplySFXVariation(source);
            source.PlayOneShot(clip);
        }

        public void PlaySFX(SFXType type, Vector3 position)
        {
            if (_isMuted) return;

            var clip = LoadClip("SFXType", type.ToString());
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] SFX clip not found: {type}");
                return;
            }

            var source = GetAvailableSFXSource();
            ApplySFXVariation(source);
            source.transform.position = position;
            source.PlayOneShot(clip);
        }

        public void PlaySFX(AudioClip clip, Vector3 position)
        {
            if (_isMuted || clip == null) return;

            var source = GetAvailableSFXSource();
            ApplySFXVariation(source);
            source.transform.position = position;
            source.PlayOneShot(clip);
        }

        public void PlaySFXFixed(SFXType type)
        {
            if (_isMuted) return;

            var clip = LoadClip("SFXType", type.ToString());
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] SFX clip not found: {type}");
                return;
            }

            var source = GetAvailableSFXSource();
            source.pitch = 1f;
            source.volume = GetSFXVolumeMultiplier();
            source.PlayOneShot(clip);
        }

        public void PlaySFX(SFXType type, float volumeScale, float pitch = 1f)
        {
            if (_isMuted) return;

            var clip = LoadClip("SFXType", type.ToString());
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] SFX clip not found: {type}");
                return;
            }

            var source = GetAvailableSFXSource();
            ApplySFXVariation(source, pitch);
            source.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        private AudioClip LoadClip(string category, string name)
        {
            string path = $"Audio/{category}/{name}";
            if (_audioCache.TryGetValue(path, out var cached))
                return cached;

            var clip = this.GetUtility<IResourceLoad>().Load<AudioClip>(path);
            if (clip != null)
                _audioCache[path] = clip;

            return clip;
        }

        private AudioSource GetAvailableSFXSource()
        {
            foreach (var source in _sfxPool)
            {
                if (!source.isPlaying) return source;
            }

            var newPool = new AudioSource[_sfxPool.Length + 1];
            Array.Copy(_sfxPool, newPool, _sfxPool.Length);
            var newSource = gameObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            newPool[_sfxPool.Length] = newSource;
            _sfxPool = newPool;
            return newSource;
        }

        private void ApplySFXVariation(AudioSource source, float basePitch = 1f)
        {
            float pitchMod = UnityEngine.Random.Range(1f - _sfxPitchVariation, 1f + _sfxPitchVariation);
            float volMod = UnityEngine.Random.Range(1f - _sfxVolumeVariation, 1f + _sfxVolumeVariation);
            source.pitch = basePitch * pitchMod;
            source.volume = GetSFXVolumeMultiplier() * volMod;
        }

        private void LoadSavedSettings()
        {
            _masterVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
            _envVol = PlayerPrefs.GetFloat("EnvVolume", 1f);
            _sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);
            _isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
            ApplyVolume();
        }

        // ===== Volume =====

        public void SetMasterVolume(float volume)
        {
            _masterVol = Mathf.Clamp01(volume);
            ApplyVolume();
        }

        public void SetBGMVolume(float volume)
        {
            _bgmVol = Mathf.Clamp01(volume);
            ApplyVolume();
        }

        public void SetEnvVolume(float volume)
        {
            _envVol = Mathf.Clamp01(volume);
            ApplyVolume();
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVol = Mathf.Clamp01(volume);
        }

        public void SetMute(bool mute)
        {
            _isMuted = mute;
            ApplyVolume();
        }

        private float GetBGMVolumeMultiplier()
        {
            return _bgmVol * _masterVol * (_isMuted ? 0f : 1f);
        }

        private float GetEnvVolumeMultiplier()
        {
            return _envVol * _masterVol * (_isMuted ? 0f : 1f);
        }

        private float GetSFXVolumeMultiplier()
        {
            return _sfxVol * _masterVol;
        }

        private void ApplyVolume()
        {
            if (_bgmSource != null)
            {
                _bgmSource.volume = GetBGMVolumeMultiplier();
            }
            if (_envSource != null)
            {
                _envSource.volume = GetEnvVolumeMultiplier();
            }
        }

        // ===== Global Control =====

        public void PauseAll()
        {
            _bgmSource.Pause();
            _envSource.Pause();
            foreach (var source in _sfxPool)
            {
                if (source.isPlaying) source.Pause();
            }
        }

        public void ResumeAll()
        {
            if (_isMuted) return;

            _envSource.UnPause();
            foreach (var source in _sfxPool)
            {
                if (source.isPlaying) source.UnPause();
            }
            _bgmSource.UnPause();
        }

        public void StopAll()
        {
            StopAllCoroutines();
            _bgmSource.Stop();
            _envSource.Stop();
            foreach (var source in _sfxPool)
            {
                source.Stop();
            }
        }

        // ===== Cleanup =====

        private void OnDestroy()
        {
            StopAllCoroutines();
            _audioCache?.Clear();
            _sfxPool = null;
        }
    }
}
