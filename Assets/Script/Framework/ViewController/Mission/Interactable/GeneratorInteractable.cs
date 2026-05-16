using QFramework.Enum;
using QFramework.ViewController.Player;
using UnityEngine;

namespace QFramework.ViewController.Mission
{
    public enum GeneratorState { Idle, Shutdown, Connected, Restarted }

    public class GeneratorInteractable : Interactable
    {
        public GeneratorState CurrentState { get; private set; } = GeneratorState.Idle;

        protected override void OnInit()
        {
            CurrentState = GeneratorState.Idle;
        }

        public override string InteractionText => CurrentState switch
        {
            GeneratorState.Idle => "关闭电源",
            GeneratorState.Connected => "重新启动",
            _ => "互动"
        };

        public override void OnInteract(GameObject player)
        {
            switch (CurrentState)
            {
                case GeneratorState.Idle:
                    CurrentState = GeneratorState.Shutdown;
                    Debug.Log("[Generator] 电源已关闭");
                    break;
                case GeneratorState.Connected:
                    CurrentState = GeneratorState.Restarted;
                    Debug.Log("[Generator] 已重新启动");
                    break;
            }
            base.OnInteract(player);
        }

        public override void OnPlaceObject()
        {
            CurrentState = GeneratorState.Connected;
            Debug.Log("[Generator] 连接件已插入");
            base.OnPlaceObject();
        }
    }
}
