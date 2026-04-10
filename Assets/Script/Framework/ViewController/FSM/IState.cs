namespace QFramework.ViewController.FSM
{
    /// <summary>
    /// 状态接口，定义状态的完整生命周期。
    /// </summary>
    public interface IState
    {
        /// <summary>进入该状态时调用（仅调用一次）。</summary>
        void OnEnter();

        /// <summary>每帧驱动，由 StateMachine.Update() 调用。</summary>
        void OnUpdate();

        /// <summary>固定帧驱动，由 StateMachine.FixedUpdate() 调用。</summary>
        void OnFixedUpdate();

        /// <summary>离开该状态时调用（仅调用一次）。</summary>
        void OnExit();

        /// <summary>
        /// 切换到该状态前执行的条件检查。
        /// 返回 false 则拒绝本次 ChangeState 请求。
        /// </summary>
        bool OnCondition();
    }
}
