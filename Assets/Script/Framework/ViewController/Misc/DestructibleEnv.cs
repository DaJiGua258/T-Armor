using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

namespace QFramework.ViewController
{
    public enum DeathMode
    {
        Hide,
        Gray,
    }

    public class DestructibleEnv : MonoBehaviour
    {
        public Action OnDestroyed;

        [SerializeField] private int _maxHealth = 50;
        private int _currentHealth;
        private const float _grey = 0.7f;
        private Color _damagedTint = new Color(_grey, _grey, _grey, 1f);

        [SerializeField] private bool _canTakeDamage = true;
        public void SetCanTakeDamage(bool value) => _canTakeDamage = value;
        [SerializeField] private DeathMode _deathMode = DeathMode.Hide;

        private bool _isDead;
        private Transform _meshNode;
        private Transform _shadowNode;
        private Transform _colliderNode;

        // 受击闪白
        private List<Renderer> _meshRenderers;
        private MaterialPropertyBlock _flashBlock;

        // 损伤特效粒子
        private Transform _damageVfxNode;
        private List<ParticleSystem> _damageVfxParticles;

        // 受击特效粒子（可选）
        private Transform _hitVfxNode;
        private List<ParticleSystem> _hitVfxParticles;
        [SerializeField] private int _hitVfxEmitCount = 5;

        private void Awake()
        {
            _currentHealth = _maxHealth;
            _colliderNode = transform.Find("Collider");
            InitFlashEffect();
            InitDamageVFX();
            InitHitVFX();
        }

        public void TakeDamage(int damage)
        {
            if (_isDead || !_canTakeDamage) return;

            _currentHealth -= damage;
            Flash();
            PlayHitVFX();
            if (_currentHealth <= 0)
            {
                _isDead = true;
                ShowDamageVFX();
                OnDeath();
            }
        }

        protected virtual void OnDeath()
        {
            ApplyDeathMode();
            UpdateNavGraph();
            OnDestroyed?.Invoke();
        }

        private void UpdateNavGraph()
        {
            if (AstarPath.active == null) return;
            var col = _colliderNode?.GetComponent<Collider2D>();
            if (col == null) return;
            var guo = new GraphUpdateObject(col.bounds);
            guo.modifyWalkability = true;
            guo.setWalkability = true;
            AstarPath.active.UpdateGraphs(guo);
        }

        private void ApplyDeathMode()
        {
            if (_meshRenderers == null || _meshRenderers.Count == 0) return;

            // 停止闪白协程，防止其覆盖死亡状态
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }

            switch (_deathMode)
            {
                case DeathMode.Hide:
                    if (_meshNode != null) _meshNode.gameObject.SetActive(false);
                    if (_shadowNode != null) _shadowNode.gameObject.SetActive(false);
                    if (_colliderNode != null) _colliderNode.gameObject.SetActive(false);
                    break;
                case DeathMode.Gray:
                    if (_meshNode == null) break;
                    var grayRenderers = _meshNode.GetComponentsInChildren<Renderer>(true);
                    var block = new MaterialPropertyBlock();
                    block.SetColor("_Color", _damagedTint);
                    block.SetFloat("_FlashAmount", 0f);
                    foreach (var r in grayRenderers)
                        r.SetPropertyBlock(block);
                    break;
            }
        }

        #region ----- 受击闪白 -------------------------

        private void InitFlashEffect()
        {
            _meshNode = transform.Find("Mesh");
            _meshRenderers = new List<Renderer>();
            if (_meshNode != null)
                GetComponentsInChildren(true, _meshRenderers);
            _flashBlock = new MaterialPropertyBlock();
            _shadowNode = transform.Find("Shdaow");
            if (_shadowNode == null) _shadowNode = transform.Find("Shadow");
        }

        private Coroutine _flashCoroutine;
        private const float FlashDuration = 0.12f;

        public void Flash()
        {
            if (_meshRenderers == null || _meshRenderers.Count == 0) return;

            if (_flashCoroutine != null)
                StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            float timer = FlashDuration;

            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                float t = timer / FlashDuration + 0.5f;

                _flashBlock.SetFloat("_FlashAmount", t);
                foreach (var r in _meshRenderers)
                {
                    if (r != null)
                        r.SetPropertyBlock(_flashBlock);
                }
                yield return null;
            }

            foreach (var r in _meshRenderers)
            {
                if (r != null)
                    r.SetPropertyBlock(null);
            }
        }

        #endregion

        #region ----- 死亡粒子特效 -------------------------

        private void InitDamageVFX()
        {
            Transform damageVfxT = transform.Find("DamagedVFX");
            _damageVfxNode = damageVfxT;
            if (damageVfxT != null)
            {
                _damageVfxParticles = new List<ParticleSystem>(damageVfxT.GetComponentsInChildren<ParticleSystem>(true));
                HideDamageVFX();
            }
            else
            {
                _damageVfxParticles = new List<ParticleSystem>();
            }
        }

        public void ShowDamageVFX() => SetDamageVfxEmission(true);

        public void HideDamageVFX() => SetDamageVfxEmission(false);

        private void SetDamageVfxEmission(bool enable)
        {
            if (_damageVfxParticles == null) return;
            foreach (var ps in _damageVfxParticles)
            {
                if (ps == null) continue;
                var emission = ps.emission;
                emission.enabled = enable;
                if (enable) ps.Play();
            }
        }

        #endregion

        #region ----- 受击特效粒子（可选） -------------------------

        private void InitHitVFX()
        {
            Transform hitVfxT = transform.Find("HitVFX");
            _hitVfxNode = hitVfxT;
            if (hitVfxT != null)
            {
                _hitVfxParticles = new List<ParticleSystem>(hitVfxT.GetComponentsInChildren<ParticleSystem>(true));
            }
            else
            {
                _hitVfxParticles = new List<ParticleSystem>();
            }
        }

        public void PlayHitVFX()
        {
            if (_hitVfxParticles == null) return;
            foreach (var ps in _hitVfxParticles)
            {
                if (ps == null) continue;
                ps.Emit(_hitVfxEmitCount);
            }
        }

        #endregion
    }
}
