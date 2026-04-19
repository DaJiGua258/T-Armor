using Unity.Mathematics;
using UnityEngine;
using QFramework.Model;
using QFramework.Utility;
using QFramework.UtilityKit;
using QFramework.ViewController.FSM;
using QFramework.Event;

namespace QFramework.ViewController.Player
{
    public class PlayerController : OverrideMonoSingleton<PlayerController>
    {
        private IPlayerModel _playerModel => this.GetModel<IPlayerModel>();

        [SerializeField] private Vector2 _targetPos;
        [Header("武器引用")]
        [SerializeField] private WeaponController _weapon;

        [Header("躯干引用")]
        [SerializeField] private Transform _body;
        [SerializeField] private Rigidbody2D _rigid;
        public float MoveSpeed;
        public float AimZOffsetDeg;
        

        [Header("脚部引用")]
        public Transform LegFl; // 前左
        public Transform LegFr; // 前右
        public Transform LegBr; // 后右
        public Transform LegBl; // 后左

        [Header("腿部调整参数")]
        public float FB;
        public float RL;
        public float LegRotation;

        [Header("惯性参数")]
        public float inertiaSpeed = 8f;             // 惯性插值速度
        public float inertiaDistance = 1f;          // 停止后惯性最大距离
        public float minMoveThreshold = 0.01f;      // 认为已经停止的阈值
        public float inertiaDelay = 0.7f;           // 滞后感倍数可调
        public Vector3 lastBodyDir;                 // 上一次身体移动方向
        
        private Vector3 _legInertiaOffset;           // 腿部惯性偏移
        private float _legInertiaFactor;             // 腿部惯性因子
        [Header("腿部偏移权重")]
        [SerializeField] private float _lerpFactor = 30;               // 躯干移动方向插值因子
        [SerializeField] private float _legOffsetWeightMulti = 1f;          // 权重倍率
        private Vector3 lastBodyPosition;           // 上一次身体位置
        private Vector3 inertiaOffset;              // 惯性偏移
        [Header("状态机")]
        private StateMachine<PlayerController> _fsm;


        protected override void Awake()
        {
            base.Awake();

            if (!_body) _body = transform.Find("Body");

            
            // 获取脚部
            Transform legs = transform.Find("Legs");
            if (!LegFl) LegFl = legs.Find("FL");
            if (!LegFr) LegFr = legs.Find("FR");
            if (!LegBr) LegBr = legs.Find("BR");
            if (!LegBl) LegBl = legs.Find("BL");

            _weapon = GetComponent<WeaponController>();
            _rigid = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            ParamsInit();
            InitLegPostion();
            
            // 初始化状态字典
            _fsm = new StateMachine<PlayerController>();
            _fsm.AddState(new PlayerIdelState(this, _fsm));
            _fsm.AddState(new PlayerMoveState(this, _fsm));
            _fsm.AddState(new PlayerDashState(this, _fsm));
            _fsm.AddState(new PlayerDeathState(this, _fsm));
            _fsm.StartState<PlayerIdelState>();

            // 死亡状态注册
            _playerModel.CurrentHealth.RegisterOnValueChanged(
                (value) =>
                {
                    if(value <= 0)
                    {
                        _fsm.ChangeState<PlayerDeathState>();
                    }
                }
            );

            TypeEventSystem.Global.Register<PlayerEvent.UpdateTarget>(
                e => UpdateTargetPos(e.Target)
            );
        }

        private void Update()
        {
            // 死亡后状态不更新
            if(_fsm.CurrentStateType == typeof(PlayerDeathState))
            {
                return;
            }

            _fsm.Update();
        
            if(_fsm.CurrentStateType != typeof(PlayerDeathState))
            {
                RotateBody();           // 旋转躯干
                UpdateLegPostion();     // 更新腿部位置
                WeaponInput();          // 武器输入
            }

            TypeEventSystem.Global.Send(new UpdatePos { Pos = transform.position });
        }

        private void FixedUpdate()
        {
            // 死亡后状态不更新
            if(_fsm.CurrentStateType == typeof(PlayerDeathState))
            {
                return;
            }

            _fsm.FixedUpdate();
        }

        /// <summary>
        /// 初始化参数
        /// </summary>
        private void ParamsInit()
        {
            MoveSpeed = _playerModel.Speed.Value;
        }


        /// <summary>
        /// 移动
        /// </summary>
        public void Move()
        {
            // 获取键盘输入的移动方向
            Vector2 input = InputUtility.GetMovementDir();
            // 如果输入方向的平方大于1，则归一化
            if (input.sqrMagnitude > 1f) input.Normalize();
                _rigid.velocity = new Vector3(input.x, input.y, 0) * MoveSpeed;
        }

        public void Dash()
        {
            _rigid.velocity = InputUtility.GetMovementDir() * MoveSpeed * 3f;
        }


        public void StopMovement()
        {
            if (_rigid != null)
                _rigid.velocity = Vector2.zero;
        }

        public void WeaponInput()
        {
            // 左手输入
            if(InputUtility.GetLeftReloadInput())
            {
                _weapon.WeaponLeft.ReloadByInput();
            }
            else if(InputUtility.GetShootLeftInput())
            {
                _weapon.WeaponLeft.Shoot();
            }
            
            // 右手输入
            if(InputUtility.GetRightReloadInput())
            {
                _weapon.WeaponRight.ReloadByInput();
            }
            else if(InputUtility.GetShootRightInput())
            {
                _weapon.WeaponRight.Shoot();
            }
        }

        public void UpdateTargetPos(Vector2 targetPos)
        {
            _targetPos = targetPos;
        }

        /// <summary>
        /// 旋转躯干
        /// </summary>
        public void RotateBody()
        {
            if (!_body)
                return;

            Vector3 hit = _targetPos;
            
            if(hit == Vector3.zero)
                return;

            // 计算点击位置与角色位置的差值
            Vector3 dir = hit - _body.position;
            dir.z = 0f;
            float dist = Vector2.Distance(hit, _body.position);


            // 在这里实现旋转平滑效果
            // 当前Body的朝向（欧拉角z)
            float currentZ = _body.rotation.eulerAngles.z;
            // 计算目标朝向
            float targetZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + AimZOffsetDeg;

            // 在360度环绕下插值
            float smoothZ = Mathf.LerpAngle(currentZ, targetZ, 10f * Time.deltaTime);

            _body.rotation = Quaternion.Euler(0f, 0f, smoothZ);

            // 当Body旋转到目标朝向时，小于10度，则允许Weapon旋转
            if(MathTool.GetAngleDifference(currentZ, targetZ) < 10f && dist > 1f)
            {
                _weapon.RotateWeapon(hit, AimZOffsetDeg);
            }

            return;
        }

        /// <summary>
        /// 更新腿部位置
        /// </summary>
        public void InitLegPostion()
        {
            if (!LegFl || !LegFr || !LegBr || !LegBl) return;

            // FL (前左): y增加(前), x减少(左)
            LegFl.SetLocalPositionAndRotation(new Vector3(-RL, FB, LegFl.localPosition.z), Quaternion.Euler(0, 0, LegRotation));

            // FR (前右): y增加(前), x增加(右)
            LegFr.SetLocalPositionAndRotation(new Vector3(RL, FB, LegFr.localPosition.z), Quaternion.Euler(0, 0, -LegRotation));

            // BR (后右): y减少(后), x增加(右)
            LegBr.SetLocalPositionAndRotation(new Vector3(RL, -FB, LegBr.localPosition.z), Quaternion.Euler(0, 0, LegRotation));

            // BL (后左): y减少(后), x减少(左)
            LegBl.SetLocalPositionAndRotation(new Vector3(-RL, -FB, LegBl.localPosition.z), Quaternion.Euler(0, 0, -LegRotation));
        }

        /// <summary>
        /// 更新腿部位置
        /// </summary>
        void UpdateLegPostion()
        {
            
        }
    }
}