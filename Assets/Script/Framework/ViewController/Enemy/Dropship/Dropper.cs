using System;
using UnityEngine;
using QFramework.ViewController.FSM;
using System.Collections.Generic;
using QFramework.Enum;

namespace QFramework.ViewController.Enemy
{
    public class Dropper : AbstractEnemy
    {
        [Header("Dropper参数")]
        [SerializeField] private List<CargoSlot> _cargoSlots;


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
            InitEnemy();
            LockCargos();
        }


        protected override void Update()
        {
            _fsm.Update();
            CarryCargos();
        }

        public override void Attack()
        {
            // 留给具体状态调用（例如进入 DropperDroppingState 时触发）
        }

        public override void Shoot()
        {
            // 留给后续实现投放/发射逻辑
        }

        #region ----- 挂载 -------------------------
        public void CarryCargos()
        {
            float height = Mesh.localPosition.y;
            foreach (var cargoSlot in _cargoSlots)
            {
                // cargoSlot.Enemy.transform.position = cargoSlot.Slot.position;
                // Vector3 pos = cargoSlot.Enemy.transform.position;  // 搭载的敌人当前位置
                cargoSlot.Enemy.transform.position = new Vector3(
                    cargoSlot.Slot.position.x,
                    cargoSlot.Slot.position.y - height,
                    cargoSlot.Enemy.transform.position.z
                );
                cargoSlot.Enemy.RotateInLock(Body.localEulerAngles.z);
            }
        }

        public void LockCargos()
        {
            if(_cargoSlots.Count == 0) return;

            foreach (var cargoSlot in _cargoSlots)
            {
                cargoSlot.Enemy.ChangeState<EnemyLockState>();
            }
        }

        public void DropCargos()
        {
            if(_cargoSlots.Count == 0) return;

            foreach (var cargoSlot in _cargoSlots)
            {
                cargoSlot.Enemy.ChangeState<EnemyFallState>();
            }
        }

        #endregion

        public void InitEnemy()
        {
            float height = Mesh.localPosition.y;

            var res = ResourceLoad.Load<GameObject>("Prefab/Enemy/" + EnemyTypeEnum.Warrior_AR.ToString());
            foreach (var cargoSlot in _cargoSlots)
            {
                
                var obj = Instantiate(res);
                var enemy = obj.GetComponent<AbstractEnemy>();
                cargoSlot.Enemy = enemy;

                enemy.transform.rotation = Quaternion.identity;
                enemy.transform.position = new Vector3(
                    transform.position.x,
                    transform.position.y - height,
                    transform.position.z
                );
                enemy.Mesh.localPosition = new Vector3(
                    0, 
                    height - 0.1f,  // 保持搭载的敌人的mesh和运输船的mesh高度一致
                    cargoSlot.Slot.localEulerAngles.z);
                
                
            }
        }

        
    }

    [Serializable]
    public class CargoSlot
    {
        public Transform Slot;
        public AbstractEnemy Enemy;
    }
}