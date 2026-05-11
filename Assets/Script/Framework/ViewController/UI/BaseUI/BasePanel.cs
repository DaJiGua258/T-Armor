using UnityEngine;
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
    public abstract class AbstractBasePanel : BaseUIComponent, IBasePanel
    {
        public void Show()
        {
            gameObject.SetActive(true);
            OnShow();
            RebuildLayout();
        }

        public void Hide()
        {
            OnHide();
            RebuildLayout();
            gameObject.SetActive(false);
        }

        private void RebuildLayout()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        }

        public virtual void OnInit() { }

        public virtual void OnShow() { }

        public virtual void OnHide() { }

        public virtual void OnClose() { }
    }
}
