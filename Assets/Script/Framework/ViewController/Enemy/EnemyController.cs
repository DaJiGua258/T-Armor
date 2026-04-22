using DG.Tweening;
using QFramework.Command;
using QFramework.Enum;
using QFramework.System;
using QFramework.ViewController.FSM;
using QFramework.ViewController.Player;
using Unity.VisualScripting;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public class EnemyController : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        public IEnemyInstanceSystem EnemyInstanceSystem => this.GetSystem<IEnemyInstanceSystem>();

        [Header("实例标识")]
        public int enemyId;
        public EnemyTypeEnum enemyEnum;

        [Header("AI 感知范围")]
        public float DetectionRange;
        public float AttackMaxRange;
        public float AttackMinRange;

        [Header("移动")]
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private Rigidbody2D _rigidbody;

        [Header("玩家引用（可留空，运行时自动查找）")]
        public Transform Target;

        [Header("引用")]
        [SerializeField] private GameObject _pf_bullet;
        private static Material s_deathMaterial;
        private static Material s_meshMaterial;

        public Transform Mesh;
        public Transform DeathObejct;
        public Transform Muzzle;
        public Collider2D Collider;
        [Header("特殊引用")]
        public LightningBolt Light;

        private StateMachine<EnemyController> _fsm;
        private Tweener _tweener;


        void Start()
        {
            // ----- 添加实例 -------------------------
            enemyId = this.SendCommand(new EnemyCommand.Add(enemyEnum, enemyId));

            if (Target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) Target = player.transform;
            }
            
            
            // ----- 初始化 -------------------------
            InitFSM();
            InitDeathObject();
            InitTransofrm();
        }

        void Update()
        {
            _fsm.Update();


            if(EnemyInstanceSystem.GetData(enemyId).CurrentHealth.Value <= 0)
            {
                _fsm.ChangeState<EnemyDeathState>();
            }
        }

        void FixedUpdate()
        {
            _fsm.FixedUpdate();
        }

        #region  ----- 初始化 -------------------------

        private void InitFSM()
        {
            _fsm = new StateMachine<EnemyController>();
            _fsm.AddState(new EnemyIdleState(this, _fsm));
            _fsm.AddState(new EnemyMoveState(this, _fsm));
            _fsm.AddState(new EnemyAttackState(this, _fsm));
            _fsm.AddState(new EnemyDeathState(this, _fsm));

            // ----- 启动状态机 -------------------------
            _fsm.StartState<EnemyIdleState>();   
        }

        private void InitTransofrm()
        {
            Mesh = transform.Find("Mesh");
            DeathObejct = transform.Find("DeathObject");
            Muzzle = transform.Find("Weapon/Muzzle");

            Collider = transform.GetComponent<Collider2D>();
        }

        #endregion

        #region ----- 移动相关 -------------------------
        /// <summary>
        /// 判断是否在检测范围内
        /// </summary>
        public bool IsInDetectRange()
        {
            if (Target == null) return false;

            float dis = Vector2.Distance(transform.position, Target.position);
            if(dis <= DetectionRange)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 判断是否在攻击范围内
        /// </summary>
        public bool IsInAttackMaxRange()
        {
            if (Target == null) return false;

            float dis = Vector2.Distance(transform.position, Target.position);
            if(dis <= AttackMaxRange)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 判断是否在指定范围内
        /// </summary>
        public bool IsInSpecifiedRange(float range)
        {
            if (Target == null) return false;
            float dis = Vector2.Distance(transform.position, Target.position);
            if(dis <= range)
            {
                return true;
            }

            return false;
        }

        public void MoveToward(Vector3 targetPos)
        {
            Vector2 direction = (targetPos - transform.position).normalized;
            _rigidbody.velocity = direction * _moveSpeed;
            Rotate(targetPos);
        }

        public void Rotate(Vector3 targetPos)
        {
            Vector2 direction = (targetPos - transform.position).normalized;
            float z = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, z);
        }

        public void StopMovement()
        {
            _rigidbody.velocity = Vector2.zero;
        }

        #endregion

        #region ----- 死亡相关 -------------------------

        public void InitDeathObject()
        {
            if(s_deathMaterial == null)
            {
                s_meshMaterial = new Material(Mesh.GetComponent<Renderer>().material);
                s_deathMaterial = new Material(s_meshMaterial);
                s_deathMaterial.name += "_death";
                float gray = 0.6f;
                s_deathMaterial.SetColor("_MainColor", new Color(gray, gray, gray, 1f));
            }
        }

        public void LockDeathObject()
        {
            DeathObejct.gameObject.transform.rotation = Quaternion.identity;
        }

        public void ActiveDeathMesh()
        {
            DeathObejct.gameObject.SetActive(true);
            Mesh.GetComponent<MeshRenderer>().material = s_deathMaterial;
            Collider.enabled = false;
        }

        public void SetDeathObjectPos(Vector3 offset)
        {
            var pos = transform.position;
            offset = (offset - pos).normalized;
            pos += offset * 0.05f;
            DeathObejct.gameObject.transform.position = pos;
        }

        #endregion

        #region ----- 武器 -------------------------

        public void Attack()
        {
            Shoot();
        }

        public void Shoot()
        {
            Light.Draw(Target);
            if(Target.TryGetComponent<PlayerController>(out PlayerController c))
            {
                this.SendCommand(PlayerCommand.Damage.Instance.Init(1));
            }
            // var obj = Instantiate(_pf_bullet, Muzzle.position, Muzzle.rotation);

        }

        #endregion

        #region ----- 杂项 -------------------------

        public void ForcePush(Vector2 forcePos, int force)
        {
            var dir = (Vector2)transform.position - forcePos;
            _rigidbody.AddForce(dir.normalized * force, ForceMode2D.Impulse);
            _rigidbody.AddTorque(force);
        }

        #endregion

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, DetectionRange);
            Gizmos.DrawWireSphere(transform.position, AttackMaxRange);
            Gizmos.DrawWireSphere(transform.position, AttackMinRange);
        }
    }
}
