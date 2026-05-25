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

        // 目标更新时，触发的目标Bar更新事件
        public struct UpdateEnemyInfo { }
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

    /// <summary>
    /// Mod 槽位数据变更（拖拽交换/取出）后触发，用于重算武器属性
    /// </summary>
    public struct ModsUpdatedEvent { }

}