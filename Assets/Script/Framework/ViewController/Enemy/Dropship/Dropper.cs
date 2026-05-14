using System;
using UnityEngine;
using QFramework.ViewController.FSM;
using System.Collections.Generic;
using System.Collections;

namespace QFramework.ViewController.Enemy
{
    public class Dropper : AbstractAirEnemy
    {
        [Header("Dropper参数")]
        [SerializeField] private List<CargoSlot> _cargoSlots;
        public bool IsDropped = false;

        [Header("运动参数")]
        [SerializeField] private float _maxSpeed = 10f;
        [SerializeField] private float _decelerationDis = 10f;
        [SerializeField] private AnimationCurve _speedCurve;
        [SerializeField] private AnimationCurve _heightCurve;
        [SerializeField] private float _meshHeight = 3f;
        public Vector3 LastPos;

        [Header("路径参数")]
        [SerializeField] private float _reachTargetDistance = 1.2f;
        private Vector3 _routeStartPoint;
        private Vector3 _routeDropPoint;
        private Vector3 _routeEndPoint;
        private bool _hasRoute;


        protected override void InitFSM()
        {
            _fsm = new StateMachine<AbstractEnemy>();
            _fsm.AddState(new DropperIdelState(this, _fsm));
            _fsm.AddState(new DropperMoveState(this, _fsm));
            _fsm.AddState(new DropperDroppingState(this, _fsm));
            _fsm.AddState(new DropperDeathState(this, _fsm));

            // 基础通用状态仍注册，避免父类 Update 中切换时报未注册。
            // _fsm.AddState(new EnemyDeathState(this, _fsm));
            // _fsm.AddState(new EnemyFallState(this, _fsm));

            _fsm.StartState<DropperIdelState>();
        }

        protected override void InitData()
        {
            base.InitData();
            LockCargos();
        }


        protected override void Update()
        {
            _fsm.Update();

            if (EnemyInstanceSystem.GetData(enemyId).CurrentHealth.Value <= 0)
            {
                _fsm.ChangeState<DropperDeathState>();
                return;
            }

            if(!IsDropped)
            {
                CarryCargos();
            }
        }

        public override void Attack()
        {
            // 留给具体状态调用（例如进入 DropperDroppingState 时触发）
        }

        public override void Shoot()
        {
            // 留给后续实现投放/发射逻辑
        }

        #region ----- 空中运动 -------------------------

        public override void MoveToward(Vector3 targetPos)
        {
            Vector2 targetDir = targetPos - transform.position;
            float disToTarget = targetDir.magnitude;
            float disFromStart = Vector2.Distance(transform.position, LastPos);

            float time = 0;
            if (disFromStart > disToTarget)
            {
                time = Mathf.Clamp01(disToTarget / _decelerationDis);
            }
            else
            {
                time = Mathf.Clamp01(disFromStart / _decelerationDis + 0.05f);
            }

            // 依据curve曲线设置速度
            Rb.velocity = _maxSpeed * _speedCurve.Evaluate(time) * targetDir.normalized;

            // 依据curve曲线设置mesh高度
            Mesh.localPosition = new Vector3(
                Mesh.localPosition.x,
                _meshHeight + _meshHeight * _heightCurve.Evaluate(time),
                Mesh.localPosition.z);

            Rotate(targetPos);
            _enterHeight = Mesh.localPosition.y;
        }

        #endregion

        #region ----- 挂载 -------------------------
        public int GetCargoSlotCount()
        {
            if (_cargoSlots == null) return 0;
            return Mathf.Min(_cargoSlots.Count, 6);
        }

        public void BindCargoEnemy(int slotIndex, AbstractEnemy enemy)
        {
            if (enemy == null) return;
            if (_cargoSlots == null || slotIndex < 0 || slotIndex >= _cargoSlots.Count) return;

            _cargoSlots[slotIndex].Enemy = enemy;

            enemy.transform.position = new Vector3(
                transform.position.x,
                transform.position.y - _meshHeight,
                transform.position.z
            );

            enemy.Mesh.localPosition = new Vector3(
                0,
                _meshHeight - 0.1f,
                enemy.Mesh.localPosition.z
            );
        }

        public void CarryCargos()
        {
            if (_cargoSlots == null || _cargoSlots.Count == 0) return;
            float height = Mesh.localPosition.y;
            foreach (var cargoSlot in _cargoSlots)
            {
                if (cargoSlot.Enemy == null || cargoSlot.Slot == null) continue;


                cargoSlot.Enemy.transform.position = new Vector3(
                    cargoSlot.Slot.position.x,
                    cargoSlot.Slot.position.y - height,
                    cargoSlot.Enemy.transform.position.z
                );

                cargoSlot.Enemy.Mesh.localPosition = new Vector3(
                    0, 
                    height - 0.1f,  // 保持搭载的敌人的mesh和运输船的mesh高度一致
                    cargoSlot.Enemy.Mesh.localPosition.z);
                
                cargoSlot.Enemy.RotateInLock(Body.localEulerAngles.z);
            }
        }

        public void LockCargos()
        {
            if (_cargoSlots == null || _cargoSlots.Count == 0) return;

            foreach (var cargoSlot in _cargoSlots)
            {
                if (cargoSlot.Enemy == null) continue;
                cargoSlot.Enemy.ChangeState<EnemyLockState>();
            }
        }

        public void DropCargos()
        {
            StartCoroutine(DropCargosEnumerator());
        }

        public void KillAllCargos()
        {
            if (_cargoSlots == null) return;
            foreach (var slot in _cargoSlots)
            {
                if (slot.Enemy != null)
                    slot.Enemy.ChangeState<EnemyDeathState>();
            }
        }

        IEnumerator DropCargosEnumerator()
        {
            if (_cargoSlots == null || _cargoSlots.Count == 0) yield break;
            yield return new WaitForSeconds(0.5f);

            foreach (var cargoSlot in _cargoSlots)
            {
                if (cargoSlot.Enemy == null) continue;
                cargoSlot.Enemy.ChangeState<EnemyFallState>();
                yield return new WaitForSeconds(0.1f);
            }
            
            yield return new WaitForSeconds(0.5f);

            IsDropped = true;
        }

        #endregion

        #region ----- 路径 -------------------------
        public void SetupRoute(Vector3 startPoint, Vector3 dropPoint, Vector3 endPoint)
        {
            _routeStartPoint = startPoint;
            _routeDropPoint = dropPoint;
            _routeEndPoint = endPoint;
            _hasRoute = true;
            LastPos = _routeStartPoint;
        }

        public bool IsRouteReady()
        {
            return _hasRoute;
        }

        public Vector3 GetCurrentRouteTarget()
        {
            return IsDropped ? _routeEndPoint : _routeDropPoint;
        }

        public bool IsReachCurrentRouteTarget()
        {
            if (!_hasRoute) return false;
            return IsInSpecifiedRange(GetCurrentRouteTarget(), _reachTargetDistance);
        }

        public void DespawnAtRouteEnd()
        {
            Destroy(gameObject);
        }
        #endregion

        
    }

    [Serializable]
    public class CargoSlot
    {
        public Transform Slot;
        public AbstractEnemy Enemy;
    }
}