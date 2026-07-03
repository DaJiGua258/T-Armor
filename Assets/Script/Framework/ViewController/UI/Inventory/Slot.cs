using QFramework.Model;
using QFramework.System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using QFramework.Event;
using QFramework.Enum;

namespace QFramework.ViewController.UI
{
    public class Slot : BaseUIComponent, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int Index;

        [SerializeField] private Image _image;
        [SerializeField] private Text _text;
        [SerializeField] private GameObject _countNode;
        private static GameObject _dragIcon;

        // ----- 绑定的数据 -----
        private ItemDataModel _itemData;

        // ----- 跨 Slot 拖拽交换状态 -----
        private static ItemDataModel _dragItemData;
        private static int _dragFromIndex;

        // ----- 悬停+F 删除 Mod -----
        private static Slot _hoveredSlot;

        void Start()
        {
            if (_dragIcon == null)
            {
                var inventoryPanel = GetComponentInParent<InventoryPanel>();
                if (inventoryPanel != null)
                    _dragIcon = inventoryPanel.transform.Find("DragSlot").gameObject;
            }
        }

        /// <summary>
        /// 绑定数据源，Slot 后续读写均通过此引用
        /// </summary>
        public void Bind(ItemDataModel itemData)
        {
            _itemData = itemData;
        }

        /// <summary>
        /// 从绑定的数据源刷新 UI
        /// </summary>
        public void UpdateSlot()
        {
            if (_itemData == null || _itemData.ItemType == ItemTypeEnum.None)
            {
                _image.gameObject.SetActive(false);
                if (_countNode != null)
                    _countNode.SetActive(false);
                return;
            }

            _image.gameObject.SetActive(true);
            _image.sprite = ResourceLoad.Load<Sprite>(_itemData.iconPath);

            if (_countNode != null)
                _countNode.SetActive(_itemData.Count.Value > 1);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hoveredSlot = this;

            if (_itemData != null)
            {
                TypeEventSystem.Global.
                    Send<UpdateViewerEvent>(new UpdateViewerEvent
                    {
                        itemData = _itemData
                    });
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_hoveredSlot == this)
                _hoveredSlot = null;
        }

        void Update()
        {
            if (_hoveredSlot != this) return;
            if (!Input.GetKeyDown(KeyCode.F)) return;
            if (_itemData == null || _itemData.ModData == null) return;

            // 清空当前槽位的 Mod
            _itemData.ItemType = ItemTypeEnum.None;
            _itemData.name = "空";
            _itemData.iconPath = string.Empty;
            _itemData.description = string.Empty;
            _itemData.ModData = null;
            _itemData.Count.Value = 0;

            UpdateSlot();

            // 通知武器系统重算属性
            TypeEventSystem.Global.Send<ModsUpdatedEvent>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_dragIcon == null) return;
            if (_itemData == null || _itemData.ItemType == ItemTypeEnum.None) return;

            _dragFromIndex = Index;
            _dragItemData = _itemData;

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
            if (_dragIcon != null && _dragIcon.activeSelf)
                _dragIcon.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragIcon == null) return;
            _dragIcon.SetActive(false);

            if (eventData.pointerEnter != null)
            {
                var targetSlot = eventData.pointerEnter.GetComponentInParent<Slot>();
                if (targetSlot != null && _dragItemData != null && targetSlot._itemData != null)
                {
                    _dragItemData.SwapWith(targetSlot._itemData);

                    // 刷新拖拽双方显示
                    UpdateSlot();
                    targetSlot.UpdateSlot();

                    // 通知武器系统重算属性
                    TypeEventSystem.Global.Send<ModsUpdatedEvent>();
                }
            }
        }
    }
}
