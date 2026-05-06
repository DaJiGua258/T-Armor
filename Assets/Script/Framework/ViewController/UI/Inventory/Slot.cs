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
        Hotbar,
    }

    public class Slot : BaseUIComponent, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int Index;
        public SlotType SlotType;

        [SerializeField] private Image _image;
        [SerializeField] private Text _text;
        private static GameObject _dragIcon;

        // ----- 跨 Slot 拖拽交换状态（静态，避免耦合到 Manager） -----
        private static int _dragFromIndex;
        private static SlotType _dragFromSlotType;
        private static int _dragToIndex;
        private static SlotType _dragToSlotType;

        void Start()
        {
            if (_dragIcon == null)
            {
                var inventoryPanel = GetComponentInParent<InventoryPanel>();
                if (inventoryPanel != null)
                    _dragIcon = inventoryPanel.transform.Find("DragSlot").gameObject;
            }
        }

        public void UpdateSlot(ItemDataModel itemData)
        {
            if (itemData.TypeEnum == TypeEnum.None)
            {
                _image.gameObject.SetActive(false);
                _text.gameObject.SetActive(false);
                return;
            }

            _image.sprite = ResourceLoad.Load<Sprite>(itemData.iconPath);
            _text.text = itemData.Count.Value.ToString();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            TypeEventSystem.Global.
                Send<UpdateViewerEvent>(new UpdateViewerEvent
                {
                    itemData = InvenotrySystem.GetInventoryItemByIndex(Index)
                });
        }

        public void OnPointerExit(PointerEventData eventData) { }

        public void OnBeginDrag(PointerEventData eventData)
        {
            var itemData = InvenotrySystem.GetInventoryItemByIndex(Index);
            if (itemData.TypeEnum == TypeEnum.None) return;

            _dragFromIndex = Index;
            _dragFromSlotType = SlotType;

            _dragIcon.SetActive(true);
            _dragIcon.GetComponent<RectTransform>().sizeDelta
                = GetComponent<RectTransform>().sizeDelta;
            _dragIcon.transform.Find("Icon_Img").
                GetComponent<Image>().sprite = _image.sprite;
            _dragIcon.transform.Find("Num_Txt").
                GetComponent<Text>().text = _text.text;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragIcon.activeSelf)
                _dragIcon.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragIcon.SetActive(false);

            if (eventData.pointerEnter != null)
            {
                var slot = eventData.pointerEnter.GetComponentInParent<Slot>();
                if (slot != null)
                {
                    _dragToIndex = slot.Index;
                    _dragToSlotType = slot.SlotType;

                    this.SendCommand(new UICommand.ExchangeSlot(
                        _dragFromIndex, _dragFromSlotType,
                        _dragToIndex, _dragToSlotType));
                }
            }
        }
    }
}
