using QFramework.Model;
using QFramework.System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using QFramework.Event;
using QFramework.Enum;
using QFramework.Manager;
using QFramework.Command;

namespace QFramework.ViewController.UI
{
    public enum SlotType
    {
        None,
        Bag,
    } 

    public class Slot : AbstractBasePanel
    {
        public int Index;
        public SlotType SlotType;

        private Image _image;  // 图标
        private Text _text;  // 数量文字

        // 拖拽时跟随鼠标的临时图标
        private static GameObject _dragIcon;
        private static Image      _dragIconImage;

        private UIManager _instance;



        void Awake()
        {
            _image = transform.Find("Icon_Img").GetComponent<Image>();
            _text = transform.Find("Num_Txt").GetComponent<Text>();
        }

        void Start()
        {
            _instance = UIManager.Instance;
        }

        public void UpdateSlot(ItemDataModel itemData)
        {
            if(itemData.TypeEnum == TypeEnum.None)
            {
                _image.gameObject.SetActive(false);
                _text.gameObject.SetActive(false);
                return;
            }
            else
            {
                _image.sprite = ResourceLoad.Load<Sprite>(itemData.iconPath);
                _text.text = itemData.Count.Value.ToString();
            }


        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            TypeEventSystem.Global.
                Send<UpdateViewerEvent>(new UpdateViewerEvent
                {
                    itemData = InvenotrySystem.GetInventoryItemByIndex(Index)
                });
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            Debug.Log("OnPointerExit");
        }



        public override void OnBeginDrag(PointerEventData eventData)
        {
            var itemData = InvenotrySystem.GetInventoryItemByIndex(Index);
            if(itemData.TypeEnum == TypeEnum.None)
            {
                return;
            }
            else
            {
                UIManager.Instance.currentIndex = Index;
                UIManager.Instance.currentSlotType = SlotType;
            }

            // 创建拖拽图标
            _instance.DragSlot.SetActive(true);

            _instance.DragSlot.GetComponent<RectTransform>().sizeDelta 
                = this.GetComponent<RectTransform>().sizeDelta;

            _instance.DragSlot.transform.Find("Icon_Img").
                GetComponent<Image>().sprite = _image.sprite;
            
            _instance.DragSlot.transform.Find("Num_Txt").
                GetComponent<Text>().text = _text.text;
            
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if(_instance.DragSlot.activeSelf)
            {
                _instance.DragSlot.transform.position = eventData.position;
            }
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if(_instance.DragSlot.activeSelf)
            {
                _instance.DragSlot.SetActive(false);
            }
            
            if(eventData.pointerEnter != null)
            {
                
                var slot = eventData.pointerEnter.GetComponentInParent<Slot>();
                if(slot != null)
                {
                    _instance.targetIndex = slot.Index;
                    _instance.targetSlotType = slot.SlotType;

                    this.SendCommand(new UICommand.ExchangeSlot(
                        _instance.currentIndex,
                        _instance.currentSlotType,
                        _instance.targetIndex,
                        _instance.targetSlotType));
                }
            }

        }

    }
}