using UnityEngine;
using QFramework.Utility;

namespace QFramework.ViewController.Misc
{
    /// <summary>
    /// 子弹抽象基类，封装公共字段与爆炸回收逻辑。
    /// 子类只需实现 Detect() 定义不同的检测方式。
    /// </summary>
    public abstract class AbstractBullet : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        [Header("组件")]
        [SerializeField] protected LayerMask _layerMask;  // 碰撞检测层级
        [SerializeField] protected Rigidbody2D _rb;  // 物理组件
        [SerializeField] protected Transform _bulletMesh;  // 子弹模型
        [SerializeField] protected GameObject _pf_bulletExplosionVFX;  // 爆炸特效预制体

        protected bool _hasExploded;  // 是否已爆炸
        protected int _damage;  // 伤害值

        protected static readonly string[] _hitTags = { "Player", "Enemy", "Env" };

        protected bool TagMatches(string tag)
        {
            for (int i = 0; i < _hitTags.Length; i++)
            {
                if (_hitTags[i] == tag) return true;
            }
            return false;
        }

        void FixedUpdate()
        {
            if (_hasExploded) return;
            Detect();
        }

        /// <summary>
        /// 子类实现具体的检测逻辑。
        /// </summary>
        protected abstract void Detect();

        /// <summary>
        /// 初始化方向飞行参数。
        /// </summary>
        /// <param name="direction">飞行方向</param>
        /// <param name="speed">飞行速度</param>
        /// <param name="damage">命中伤害</param>
        public virtual void InitBullet(Vector3 direction, int speed, int damage)
        {
            _damage = damage;
            _hasExploded = false;
            _bulletMesh.gameObject.SetActive(true);
            _rb.velocity = ((Vector2)direction).normalized * speed;
        }

        /// <summary>
        /// 执行爆炸：隐藏模型、生成特效、回收对象。
        /// </summary>
        protected void Explode(Vector3 hitPos)
        {
            if (_hasExploded) return;
            _hasExploded = true;

            _rb.velocity = Vector2.zero;
            _bulletMesh.gameObject.SetActive(false);

            var ob = this.GetUtility<IObjectPoolUtility>();
            var timer = this.GetUtility<ITimerUtility>();

            GameObject explosionVFX = ob.GetObject(_pf_bulletExplosionVFX, hitPos, GetExplosionRotation());

            timer.AddOnce(
                () => ob.PushObject(explosionVFX),
                1f,
                () => ob.PushObject(gameObject)
            );
        }

        /// <summary>
        /// 子类可覆写以自定义爆炸特效的旋转。
        /// </summary>
        protected virtual Quaternion GetExplosionRotation()
        {
            Vector3 euler = transform.rotation.eulerAngles;
            euler.z += 180f;
            return Quaternion.Euler(euler);
        }
    }
}
