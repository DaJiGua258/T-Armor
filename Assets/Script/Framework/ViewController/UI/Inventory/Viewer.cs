using QFramework.Enum;
using QFramework.Event;
using QFramework.System;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class Viewer : AbstractBasePanel
    {
        private Text _descriptionText;

        void Awake()
        {
            _descriptionText = transform.Find("Description/Description_Tex").GetComponent<Text>();
        }

        void Start()
        {
            
        }

        public void UpdateViewer(ItemDataModel itemData)
        {
            if(itemData.TypeEnum == TypeEnum.None)
            {
                return;
            }
            _descriptionText.text = itemData.description;
        }
    }
}