using System.Collections;
using UnityEngine;

namespace QFramework.ViewController.Player
{
    public class PlayerDeathVFXController : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _vfx_explosionTiny1;
        [SerializeField] private ParticleSystem _vfx_explosionTiny2;
        [SerializeField] private ParticleSystem _vfx_explosionTiny3;
        [SerializeField] private ParticleSystem _vfx_explosionSmall;
        [SerializeField] private ParticleSystem _vfx_crashFire;

        void Start()
        {
            _vfx_explosionTiny1.Stop();
            _vfx_explosionTiny2.Stop();
            _vfx_explosionTiny3.Stop();
            _vfx_explosionSmall.Stop();
            // _vfx_crashFire.Stop();
        }

        public void PlayVFX()
        {
            if (!this || !isActiveAndEnabled) return;
            StartCoroutine(PlayVFXCoroutine());
        }

        IEnumerator PlayVFXCoroutine()
        {
            _vfx_explosionTiny1.Play();
            yield return new WaitForSeconds(0.1f);
            _vfx_explosionTiny2.Play();
            yield return new WaitForSeconds(0.1f);
            _vfx_explosionTiny3.Play();
            yield return new WaitForSeconds(0.1f);
            _vfx_explosionSmall.Play();
            yield return new WaitForSeconds(0.1f);
            // if (_vfx_crashFire != null) _vfx_crashFire.Play();
        }
    }
}