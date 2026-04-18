using QFramework.Model;
using QFramework.System;
using QFramework.Utility;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace QFramework.ViewController.UI
{
    /// <summary>
    /// 所有 UI 面板的基类。
    /// 实现 IController 使面板可通过 QFramework Architecture 访问 System / Model，
    /// 并可通过 SendCommand 写入数据，保持单向数据流。
    ///
    /// 生命周期顺序：OnInit → OnShow ↔ OnHide（可重复）→ OnClose
    /// 推荐在 OnShow() 中订阅 BindableProperty / Event，在 OnHide() 中取消订阅。
    /// </summary>
    public abstract class AbstractBasePanel : MonoBehaviour, IBasePanel, IController, IUIEventBase
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        // ----- Model -------------------------
        public ILevelTypeModel LevelTypeModel => this.GetModel<ILevelTypeModel>();
        public IPlayerModel PlayerModel => this.GetModel<IPlayerModel>();
        
        // ----- System -------------------------
        public IInvenotrySystem InvenotrySystem => this.GetSystem<IInvenotrySystem>();
        public IPlayerSystem PlayerSystem => this.GetSystem<IPlayerSystem>();
        public ILevelSystem LevelSystem => this.GetSystem<ILevelSystem>();

        // ----- Utility -------------------------
        public IResourceLoad ResourceLoad => this.GetUtility<IResourceLoad>();
        public IInputUtility InputUtility => this.GetUtility<IInputUtility>();
        


        public void Show()
        {
            gameObject.SetActive(true);
            OnShow();
        }

        public void Hide()
        {
            Canvas.ForceUpdateCanvases();
            OnHide();
            gameObject.SetActive(false);
        }

        public virtual void OnInit() { }

        public virtual void OnShow() { }

        public virtual void OnHide() { }

        public virtual void OnClose() { }

        // --- 点击与按下事件 --
        public virtual void OnPointerClick(PointerEventData eventData) { }

        public virtual void OnPointerDown(PointerEventData eventData) { }

        public virtual void OnPointerUp(PointerEventData eventData) { }

        // --- 鼠标进入与移出事件 ---

        public virtual void OnPointerEnter(PointerEventData eventData) { }

        public virtual void OnPointerExit(PointerEventData eventData) { }

        // --- 拖拽事件 ---

        public virtual void OnBeginDrag(PointerEventData eventData) { }

        public virtual void OnDrag(PointerEventData eventData) { }

        public virtual void OnEndDrag(PointerEventData eventData) { }
    }
}
