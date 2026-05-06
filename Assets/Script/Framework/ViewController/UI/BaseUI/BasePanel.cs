using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace QFramework.ViewController.UI
{
    /// <summary>
    /// 所有 UI 面板的基类。
    /// 继承 BaseUIComponent 获得 IController 架构访问能力。
    ///
    /// 生命周期顺序：OnInit → OnShow ↔ OnHide（可重复）→ OnClose
    /// 推荐在 OnShow() 中订阅 BindableProperty / Event，在 OnHide() 中取消订阅。
    /// </summary>
    public abstract class AbstractBasePanel : BaseUIComponent, IBasePanel, IUIEventBase
    {


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
