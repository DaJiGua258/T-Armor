using System.Collections.Generic;
using QFramework.Enum;
using QFramework.Model;

namespace QFramework.System
{
    public interface IInvenotrySystem : ISystem
    {

    }

    public class InvenotrySystem : AbstractSystem, IInvenotrySystem
    {
        private Dictionary<int, ItemDataModel> _itemDataCache = new Dictionary<int, ItemDataModel>();

        protected override void OnInit()
        {
            
        }
    }

    public class ItemDataModel : InstanceType
    {
        private static int _itemCounter = 0;

        public ItemTypeEnum ItemType;
        public bool canStack;
        public int maxStack;
        public int Count;

        public ItemDataModel(ItemConfig itemConfig)
        {
            this.TypeEnum = TypeEnum.Item;
            this.InstanceId = GetInstanceId((int)itemConfig.ItemType, _itemCounter);
            _itemCounter++;

            this.ItemType = itemConfig.ItemType;
            this.canStack = itemConfig.canStack;
            this.maxStack = itemConfig.maxStack;
            this.Count = 0;
        }
    }

}