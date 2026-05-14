using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public abstract class AbstractAirEnemy : AbstractEnemy
    {
        [Header("悬浮参数")]
        [SerializeField] protected float _amplitude = 0.5f;              // 上下振幅
        [SerializeField] protected float _frequency = 1f;                // 上下频率
        [SerializeField] protected float _horizontalAmplitude = 0.3f;    // 水平漂移振幅
        [SerializeField] protected float _horizontalFrequency = 0.5f;    // 水平漂移频率
        protected float _enterHeight = 3f;

        private float _airFloatTimer;
        private Vector3 _originMeshLocalPos;
        private Vector3 _originShadowLocalPos;
        private bool _hasOrigin;

        #region ----- 状态机 -------------------------

        protected override void InitFSM()
        {
            base.InitFSM();
            _fsm.AddState(new AirEnemyDeathState(this, _fsm));
        }

        protected override void Update()
        {
            if (IsDead())
            {
                _fsm.ChangeState<AirEnemyDeathState>();
                _fsm.Update();
                return;
            }
            base.Update();
        }

        #endregion

        /// <summary>
        /// 使用内部计时器驱动浮动（适合 Update 中持续调用）。
        /// </summary>
        public void AirFloat()
        {
            _airFloatTimer += Time.deltaTime;
            AirFloat(_airFloatTimer);
        }

        /// <summary>
        /// 三角函数偏移实现上下浮动 + 水平缓慢飘动。
        /// 全部使用 Mesh.localPosition，不干扰 Agent 的根节点寻路。
        /// </summary>
        public void AirFloat(float timer)
        {
            if (!_hasOrigin)
            {
                _originMeshLocalPos = Mesh.localPosition;
                if (Shadow != null) _originShadowLocalPos = Shadow.localPosition;
                _hasOrigin = true;
            }

            float height = _enterHeight +
                Mathf.Sin(timer * Mathf.PI * _frequency) * _amplitude;

            float horizontalOffset =
                Mathf.Sin(timer * Mathf.PI * _horizontalFrequency) * _horizontalAmplitude +
                Mathf.Sin(timer * Mathf.PI * _horizontalFrequency * 2.3f + 0.7f) * _horizontalAmplitude * 0.4f;

            Mesh.localPosition = new Vector3(
                _originMeshLocalPos.x + horizontalOffset,
                height,
                _originMeshLocalPos.z);

            if (Shadow != null)
            {
                Shadow.localPosition = new Vector3(
                    _originShadowLocalPos.x + horizontalOffset,
                    _originShadowLocalPos.y,
                    _originShadowLocalPos.z);
            }
        }
    }
}
