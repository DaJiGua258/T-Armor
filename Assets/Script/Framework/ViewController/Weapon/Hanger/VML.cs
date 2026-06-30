using System.Collections;
using QFramework.Enum;
using QFramework.Event;
using QFramework.Manager;
using QFramework.Model;
using QFramework.Utility;
using QFramework.ViewController.Misc;
using UnityEngine;

namespace QFramework.ViewController.Player
{
    public class VML : AbstractHangerWeapon
    {
        [Header("VML 发射设置")]
        [SerializeField] private Transform[] _launchPositions;

        public override SFXType ShootSFXType => SFXType.misslie_launch;

        private int _currentIndex;
        private Vector3 _aimTargetPos;
        private Transform _targetTransform;
        private ParticleSystem[] _launchVfx;
        private bool _hasReceivedTarget;

        protected override void Start()
        {
            base.Start();

            _launchVfx = new ParticleSystem[_launchPositions.Length];
            for (int i = 0; i < _launchPositions.Length; i++)
            {
                _launchVfx[i] = _launchPositions[i].GetComponentInChildren<ParticleSystem>();
                _launchVfx[i].Stop();
            }

            TypeEventSystem.Global.Register<PlayerEvent.UpdateTarget>(e =>
            {
                _aimTargetPos = e.Target;
                _hasReceivedTarget = true;
                if (!e.HasTarget) _targetTransform = null;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            TypeEventSystem.Global.Register<WeaponEvent.GetTargetCollider>(e =>
            {
                _targetTransform = e.TargetCollider != null ? e.TargetCollider.transform : null;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        protected override void OnTrigger()
        {
            if (IsActive) return;
            if (!CanFire()) return;
            IsActive = true;
            StartCoroutine(FireRoutine());
        }

        private IEnumerator FireRoutine()
        {
            if (_launchPositions == null || _launchPositions.Length == 0)
            {
                IsActive = false;
                yield break;
            }

            float interval = 60f / WeaponDataModel.Rpm / _launchPositions.Length;

            for (int i = 0; i < _launchPositions.Length; i++)
            {
                if (WeaponDataModel.CurMagazine.Value <= 0) break;
                if (WeaponDataModel.WeaponState == WeaponStateEnum.Reloading) break;

                int launchIndex = _currentIndex;
                var launchPos = _launchPositions[launchIndex];
                _currentIndex = (_currentIndex + 1) % _launchPositions.Length;

                SpawnMissile(launchPos.position, launchIndex);
                ConsumeShot();

                if (i < _launchPositions.Length - 1)
                    yield return new WaitForSeconds(interval);
            }

            IsActive = false;
        }

        private void SpawnMissile(Vector3 pos, int launchIndex)
        {
            if (!_hasReceivedTarget)
                _aimTargetPos = this.GetUtility<IInputUtility>().GetMousePos();

            var bullet = this.GetUtility<IObjectPoolUtility>().GetObject(_pf_bullet, pos + Vector3.forward * 0.1f, Quaternion.Euler(0, 0, 90));
            _launchVfx[launchIndex].Play();

            AudioManager.Instance.PlaySFX(ShootSFXType);

            var projectile = bullet.GetComponent<Projectile>();
            var toTarget = (_aimTargetPos - pos).normalized;
            var damageInfo = new DamageInfo(WeaponDataModel.BulletDamage, WeaponDataModel.KnockbackValue, WeaponDataModel.BurnValue, toTarget, WeaponDataModel.SlowValue);
            projectile.InitProjectile(_aimTargetPos, WeaponDataModel.BulletSpeed, damageInfo);
            projectile.SetLayerMask(_bulletLayerMask);
            projectile.SetVerticalLaunch(true);

            if (_targetTransform != null)
                projectile.SetHomingTarget(_targetTransform);
            else
                projectile.SetHomingPosition(_aimTargetPos + (Vector3)UnityEngine.Random.insideUnitCircle * 1f);
        }
    }
}
