using System.Collections.Generic;
using QFramework.Command;
using QFramework.Model;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QFramework.ViewController
{
    public class Test : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture()
        {
            throw new global::System.NotImplementedException();
        }

        void Update()
    {
        // 检测鼠标左键点击
        if (Input.GetMouseButtonDown(0))
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            
            // 核心：强制要求 EventSystem 进行一次全局射线检测
            if (EventSystem.current != null)
            {
                EventSystem.current.RaycastAll(eventData, results);

                if (results.Count > 0)
                {
                    Debug.Log("==== 射线检测成功 ====");
                    for (int i = 0; i < results.Count; i++)
                    {
                        // 打印点击到的物体名称、所属层级和深度
                        Debug.Log($"[第{i}层覆盖]: {results[i].gameObject.name} (Layer: {LayerMask.LayerToName(results[i].gameObject.layer)})");
                    }
                }
                else
                {
                    Debug.LogWarning("==== 射线未碰撞到任何 UI 物体 ====");
                }
            }
            else
            {
                Debug.LogError("场景中缺少 EventSystem 组件！");
            }
        }
    }
    }
}