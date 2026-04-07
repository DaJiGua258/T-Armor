using UnityEngine;

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
    public abstract class BasePanel : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        /// <summary> 首次创建时调用一次，用于查找子节点引用、初始化状态。 </summary>
        public virtual void OnInit() { }

        /// <summary> 
        /// 每次显示时调用，在此订阅数据源。 
        /// </summary>
        public virtual void OnShow() { }

        /// <summary> 
        /// 每次隐藏时调用，在此取消数据订阅。 
        /// </summary>
        public virtual void OnHide() { }

        /// <summary> 面板被 UIManager.DestroyPanel 销毁前调用。 </summary>
        public virtual void OnClose() { }

        public void Show()
        {
            gameObject.SetActive(true);
            OnShow();
        }

        public void Hide()
        {
            OnHide();
            gameObject.SetActive(false);
        }
    }
}
