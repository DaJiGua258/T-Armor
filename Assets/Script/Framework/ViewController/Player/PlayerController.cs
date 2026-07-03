using Unity.Mathematics;
using UnityEngine;
using QFramework;
using QFramework.Model;
using QFramework.Utility;
using QFramework.UtilityKit;
using QFramework.ViewController.FSM;
using QFramework.Event;
using QFramework.ViewController.UI;
using QFramework.Manager;
using DG.Tweening;
using QFramework.Command;

namespace QFramework.ViewController.Player
{
    public class PlayerController : OverrideMonoSingleton<PlayerController>
    {
        public IPlayerModel PlayerModel => this.GetModel<IPlayerModel>();

        [SerializeField] private Vector2 _targetPos;
        [Header("武器引用")]
        [SerializeField] private WeaponController _weapon;
        [SerializeField] private HangerWeaponController _hangerWeapon;

        [Header("躯干引用")]
        [SerializeField] private Transform _body;
        [SerializeField] private Transform _legs;
        [SerializeField] private Transform _shadow;
        [SerializeField] private Transform _hitbox;
        [SerializeField] private Rigidbody2D _rigid;
        [SerializeField] private float _rotateSpeed = 10f;
        public float MoveSpeed;
        public float AimZOffsetDeg;
    
        [Header("其他引用")]
        [SerializeField] private PlayerDeathVFXController _deathVFX;

        [Header("Leg VFX")]
        private ParticleSystem[] _legParticles;

        [Header("模式状态")]
        private bool _weaponEnabled = true;

        [Header("状态机")]
        private StateMachine<PlayerController> _fsm;
        


        protected override void Awake()
        {
            base.Awake();

            _weapon = GetComponent<WeaponController>();
            _hangerWeapon = GetComponent<HangerWeaponController>();
            _rigid = GetComponent<Rigidbody2D>();

            FindLegParticles();

            // 初始化状态字典
            _fsm = new StateMachine<PlayerController>();
            _fsm.AddState(new PlayerIdelState(this, _fsm));
            _fsm.AddState(new PlayerMoveState(this, _fsm));
            _fsm.AddState(new PlayerDashState(this, _fsm));
            _fsm.AddState(new PlayerSprintState(this, _fsm));
            _fsm.AddState(new PlayerLockState(this, _fsm));
            _fsm.AddState(new PlayerDeathState(this, _fsm));
            _fsm.StartState<PlayerIdelState>();
        }

        private void Start()
        {
            ParamsInit();

            // 死亡状态注册
            PlayerModel.CurrentHealth.RegisterOnValueChanged(
                (value) =>
                {
                    if (value <= 0)
                    {
                        _fsm.ChangeState<PlayerDeathState>();
                    }
                }
            ).UnRegisterWhenGameObjectDestroyed(this);

            TypeEventSystem.Global.Register<PlayerEvent.UpdateTarget>(
                e => UpdateTargetPos(e.Target)
            ).UnRegisterWhenGameObjectDestroyed(gameObject);

            TypeEventSystem.Global.Register<PlayerEvent.SwitchAimingMode>(
                e => _weaponEnabled = e.Mode == AimingModeEnum.Combat
            ).UnRegisterWhenGameObjectDestroyed(gameObject);

            TypeEventSystem.Global.Send(new PlayerEvent.InitCompleted
            {
                PlayerTransform = transform
            });
        }

        private void Update()
        {
            if(_fsm.CurrentStateType == typeof(PlayerDeathState)
                || _fsm.CurrentStateType == typeof(PlayerLockState)
                || UIGameManager.Instance == null
                || UIGameManager.Instance.IsModalActive())
            {
                return;
            }

            _fsm.Update();
            RotateBody();           // 旋转躯干
            RotateLegs();
            WeaponInput();          // 武器输入
            CheckFuel();

            TypeEventSystem.Global.Send(new WeaponInfoEvent.UpdatePos { Pos = transform.position });
        }

        private void FixedUpdate()
        {
            if(_fsm.CurrentStateType == typeof(PlayerDeathState)
                || _fsm.CurrentStateType == typeof(PlayerLockState))
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
            MoveSpeed = PlayerModel.Speed.Value;

            if (_shadow == null)
            {
                var mesh = transform.Find("Mesh");
                if (mesh != null)
                    _shadow = mesh.Find("Shadow");
            }
        }

        #region ----- 状态动作 -------------------------

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

        public void Dash(Vector2 dir)
        {
            _rigid.velocity = dir * MoveSpeed * 4f;
        }


        public void StopMovement()
        {
            if (_rigid != null)
                _rigid.velocity = Vector2.zero;
        }

        public void Sprint()
        {
            Vector2 curVel = _rigid.velocity.normalized;
            Vector2 input = InputUtility.GetMovementDir();

            _rigid.velocity = Vector2.Lerp(curVel, input, PlayerModel.SprintSmooth * Time.deltaTime);
            _rigid.velocity *= MoveSpeed * 1.5f;
        }

        public void PlayDeathVFX()
        {
            if (_deathVFX == null) return;
            _deathVFX.PlayVFX();
        }

        private void FindLegParticles()
        {
            Transform vfxNode = _legs?.Find("vfx");
            if (vfxNode != null)
            {
                _legParticles = vfxNode.GetComponentsInChildren<ParticleSystem>();
            }
        }

        public void StopLegParticles()
        {
            if (_legParticles == null) return;
            foreach (var ps in _legParticles)
            {
                ps.Stop();
            }
        }

        public void ResumeLegParticles()
        {
            if (_legParticles == null) return;
            foreach (var ps in _legParticles)
            {
                ps.Play();
            }
        }

        public void SetLockState(bool locked)
        {
            if (locked)
                _fsm.ChangeState<PlayerLockState>();
            else
                _fsm.ChangeState<PlayerIdelState>();
        }

        #endregion

        /// <summary>
        /// 预览旋转，只旋转躯干和网格（不涉及游戏逻辑）
        /// </summary>
        public void RotatePreview(float deltaAngle)
        {
            _body.Rotate(0f, 0f, deltaAngle);
            _legs.Rotate(0f, 0f, deltaAngle);
        }

        #region ----- 常态检测 -------------------------

        public void WeaponInput()
        {
            if (!_weaponEnabled) return;

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

            // 吊架武器输入
            if (_hangerWeapon != null)
            {
                if (InputUtility.GetHangerLeftInputDown() && _hangerWeapon.WeaponLeft != null)
                    _hangerWeapon.WeaponLeft.Trigger();
                if (InputUtility.GetHangerRightInputDown() && _hangerWeapon.WeaponRight != null)
                    _hangerWeapon.WeaponRight.Trigger();
            }
        }

        public void UpdateTargetPos(Vector2 targetPos)
        {
            _targetPos = targetPos;
        }

        private void CheckFuel()
        {
            if(_fsm.CurrentStateType == typeof(PlayerIdelState)
                || _fsm.CurrentStateType == typeof(PlayerMoveState))
            {
                this.SendCommand(PlayerCommand.AddFuel.Instance.Init(PlayerModel.FuelRecovery * Time.deltaTime));
            }
        }

        #endregion

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

            if (_shadow != null)
                _shadow.rotation = _body.rotation;

            if (_hitbox != null)
            {
                _hitbox.rotation = _body.rotation;
            }

            // 当Body旋转到目标朝向时，小于10度，则允许Weapon旋转
            if(MathTool.GetAngleDifference(currentZ, targetZ) < 10f && dist > 1f)
            {
                _weapon.RotateWeapon(hit, AimZOffsetDeg);
            }

            return;
        }

        public void RotateLegs()
        {
            Vector2 velocity = _rigid.velocity;
            
            // 速度太小时不旋转，避免归零时抖动
            if (velocity.sqrMagnitude < 0.01f)
                return;

            // 计算目标角度：让 right 方向对齐 velocity
            // right 对应 angle = 0°，所以直接用 velocity 的角度
            float targetAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

            // 平滑插值
            _legs.rotation = Quaternion.Slerp(
                _legs.rotation,
                targetRotation,
                _rotateSpeed * Time.deltaTime
            );
        }

        public string GetCurrentState()
        {
            return _fsm.CurrentState switch
            {
                PlayerIdelState => "Idel",
                PlayerMoveState => "Move",
                PlayerDashState => "Dash",
                PlayerSprintState => "Sprint",
                PlayerLockState => "Lock",
                PlayerDeathState => "Death",
                _ => "Unknown"
            };
        }
    }
}