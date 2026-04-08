using QFramework.Model;
using QFramework.System;
using QFramework.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class Slot : BasePanel
    {

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
            _image.gameObject.SetActive(true);
            _text.gameObject.SetActive(true);
            _image.sprite = _resourceLoad.Load<Sprite>(itemData.iconPath);
            _text.text = itemData.Count.Value.ToString();
        }

        public void OnHideIcon()
        {
            _image.gameObject.SetActive(false);
            _text.gameObject.SetActive(false);
        }

    }
}