using QFramework.Model;
using QFramework.System;
using QFramework.Utility;
using UnityEngine;

namespace QFramework.ViewController.UI
{
    /// <summary>
    /// UI 组件的轻量基类，提供 IController 架构访问能力，不含面板生命周期。
    /// 子面板（PlayerPanel、Slot 等）应继承此类而非 AbstractBasePanel。
    /// </summary>
    public class BaseUIComponent : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        // ----- Model -------------------------
        public ILevelTypeModel LevelTypeModel => this.GetModel<ILevelTypeModel>();
        public IPlayerModel PlayerModel => this.GetModel<IPlayerModel>();

        // ----- System -------------------------
        public IInvenotrySystem InvenotrySystem => this.GetSystem<IInvenotrySystem>();
        public IPlayerSystem PlayerSystem => this.GetSystem<IPlayerSystem>();
        public ILevelSystem LevelSystem => this.GetSystem<ILevelSystem>();
        public IMissionSystem MissionSystem => this.GetSystem<IMissionSystem>();
        public IEnemyInstanceSystem EnemyInstanceSystem => this.GetSystem<IEnemyInstanceSystem>();
        // ----- Utility -------------------------
        public IResourceLoad ResourceLoad => this.GetUtility<IResourceLoad>();
        public IInputUtility InputUtility => this.GetUtility<IInputUtility>();
    }
}
