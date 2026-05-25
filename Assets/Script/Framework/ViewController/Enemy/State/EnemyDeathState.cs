using QFramework.Event;
using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public class EnemyDeathState : AbstractState<AbstractEnemy>
    {
        public EnemyDeathState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }

        public override void OnEnter()
        {
            Entity.StopMovement();
            Entity.ShowDeathVFX();

            TypeEventSystem.Global.Send(new StatsEvent.OnEnemyKilled
            {
                EnemyId = Entity.enemyId,
                Type = Entity.enemyType
            });
        }
    }
}