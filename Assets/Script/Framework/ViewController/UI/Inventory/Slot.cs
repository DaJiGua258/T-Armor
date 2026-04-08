using QFramework.Model;
using QFramework.System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using QFramework.Event;
using QFramework.Enum;

namespace QFramework.ViewController.UI
{
    public class Slot : AbstractBasePanel
    {
        public int Index;
        private Image _image;  // 图标
        private Text _text;  // 数量文字
    

        void Awake()
        {
            _image = transform.Find("Icon_Img").GetComponent<Image>();
            _text = transform.Find("Num_Txt").GetComponent<Text>();
        }

        void Start()
        {
            
        }

        public void OnInit(ItemDataModel itemData)
        {
            if(itemData.TypeEnum == TypeEnum.None)
            {
                _image.gameObject.SetActive(false);
                _text.gameObject.SetActive(false);
                return;
            }
            else
            {
                _image.sprite = _resourceLoad.Load<Sprite>(itemData.iconPath);
                _text.text = itemData.Count.Value.ToString();
            }


        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            TypeEventSystem.Global.
                Send<UpdateViewerEvent>(new UpdateViewerEvent
                {
                    itemData = _invenotrySystem.GetInventoryItemByIndex(Index)
                });
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            Debug.Log("OnPointerExit");
        }



        public override void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log("OnBeginDrag");
        }

        public override void OnDrag(PointerEventData eventData)
        {
            Debug.Log("OnDrag");
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            Debug.Log("OnEndDrag");
        }

    }
}