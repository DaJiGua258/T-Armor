using QFramework.System;
using UnityEngine;

namespace QFramework.Event
{
    #region  ---- GameHUD --------------------

    public struct RegisterWeaponInfo { }

    public struct UpdatePos 
    { 
        public Vector2 Pos; 
    }
    

    #endregion

    public struct UpdateInventoryEvent { }

    public struct UpdateViewerEvent 
    {
        public ItemDataModel itemData;
    }

}