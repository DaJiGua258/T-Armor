using UnityEngine;
using UnityEngine.EventSystems;

namespace QFramework.ViewController.UI
{
    public interface IUIEventBase : 
    IPointerClickHandler, 
    IPointerDownHandler, 
    IPointerUpHandler, 
    IPointerEnterHandler, 
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
    {

    }
}