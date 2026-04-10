namespace QFramework.ViewController.FSM
{
    /// <summary>
    /// 泛型抽象状态基类。
    /// TOwner 为拥有此状态机的宿主类型（如 EnemyController）。
    /// 子类可按需覆写生命周期方法，未覆写的方法默认为空操作。
    /// </summary>
    public abstract class AbstractState<TEntity> : IState
    {
        /// <summary>状态机的宿主对象（如持有此 FSM 的 MonoBehaviour）。</summary>
        protected TEntity Entity;

        /// <summary>所属状态机，用于在状态内部触发状态切换。</summary>
        protected StateMachine<TEntity> FSM;

        protected AbstractState(TEntity entity, StateMachine<TEntity> fsm)
        {
            Entity = entity;
            FSM = fsm;
        }

        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
        public virtual void OnExit() { }
        public virtual bool OnCondition() => true;
    }
}
