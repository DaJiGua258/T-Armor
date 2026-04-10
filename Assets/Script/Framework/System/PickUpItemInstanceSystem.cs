using System.Collections.Generic;
using QFramework.Enum;
using QFramework.Model;

namespace QFramework.System
{
    public interface IPickUpItemInstanceSystem : ISystem
    {
        public Dictionary<int, ItemDataModel> ItemDataInstanceCache { get; }

        public int AddItemInstance(ItemTypeEnum itemType);
        public ItemDataModel RemoveItemInstance(int itemInstanceId);
    }

    public class PickUpItemInstanceSystem : AbstractSystem, IPickUpItemInstanceSystem
    {
        // 拾取物的数据实例缓存
        public Dictionary<int, ItemDataModel> ItemDataInstanceCache { get; private set; } = new Dictionary<int, ItemDataModel>(200);
        private IItemConfigModel _itemDataModel => this.GetModel<IItemConfigModel>();

        protected override void OnInit()
        {
            
        }
        
        /// <summary>
        /// 
        /// </summary>
        public int AddItemInstance(ItemTypeEnum itemType)
        {
            // 获取配置
            var itemConfig = _itemDataModel.GetItemConfig(itemType);

            // 生成新的实例数据
            ItemDataModel itemData = new ItemDataModel(itemConfig);
            ItemDataInstanceCache.Add(itemData.InstanceId.Value, itemData);
            
            return itemData.InstanceId.Value;
        }

        /// <summary>
        /// 通过id移除物品数据，并返回移除的物品数据
        /// </summary>
        public ItemDataModel RemoveItemInstance(int itemInstanceId)
        {
            var itemData = ItemDataInstanceCache[itemInstanceId];
            ItemDataInstanceCache.Remove(itemInstanceId);
            return itemData;
        }
    }
}