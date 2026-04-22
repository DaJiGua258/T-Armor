using QFramework.System;
using QFramework.ViewController.UI;
using UnityEngine;

namespace QFramework.Event
{
    #region  ---- GameHUD --------------------

    public class WeaponInfoEvent
    {
        public struct Register { } // 【注册武器事件】事件

        public struct UpdateBarLeft
        {
            public WeaponDataModel data;
        }

        public struct UpdateBarRight
        {
            public WeaponDataModel data;
        }


        public struct UpdatePos 
        { 
            public Vector2 Pos; 
        }
    }

    
    /// <summary>
    /// 获取瞄准框位置事件
    /// </summary>
    public struct GetAimFramePos
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