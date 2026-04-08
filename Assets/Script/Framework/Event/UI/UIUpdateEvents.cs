using QFramework.System;

namespace QFramework.Event
{
    public struct UpdateInventoryEvent { }

    public struct UpdateViewerEvent 
    {
        public ItemDataModel itemData;
    }

}