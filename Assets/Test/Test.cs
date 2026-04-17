using System.Collections.Generic;
using QFramework.Command;
using QFramework.Event;
using QFramework.Manager;
using QFramework.Model;
using QFramework.ViewController.UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QFramework.ViewController
{
    public class Test : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        public GameObject sphere;
        public float globeRadius = 1f;
        public NodeLine linePrefab;

        // void Start()
        // {
        //     // 1. 创建一个球体作为参考
        //     GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //     sphere.transform.position = Vector3.zero;
        //     sphere.transform.localScale = Vector3.one * (globeRadius * 2f);
        //     sphere.GetComponent<Renderer>().material.color = new Color(0.2f, 0.2f, 0.2f);

        //     // 2. 随机生成两个球面上的点
        //     // 2. 随机生成两个球面上的点
        //     Vector3 p1 = Random.onUnitSphere * globeRadius;
        //     Vector3 p2 = Random.onUnitSphere * globeRadius;

        //     // 3. 生成线条
        //     if (linePrefab != null)
        //     {
        //         NodeLine line = Instantiate(linePrefab);
        //         line.Init(p1, p2, Vector3.zero);
        //     }
        //     else
        //     {
        //         Debug.LogError("请先将 NodeLine 预制体拖入 Inspector 面板！");
        //     }
        // }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                this.SendCommand(new PlayerCommand.Damage(50));
            }

            if(Input.GetKeyDown(KeyCode.B))
            {
                GameManager.Instance.SetGameResultState(GameResultState.GameFinished);
                GameManager.Instance.EnterMainScene();
            }
        }
    }
}