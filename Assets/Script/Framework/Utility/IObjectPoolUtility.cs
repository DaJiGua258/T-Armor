using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QFramework.Utility
{
    public interface IObjectPoolUtility : IUtility
    {
        public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation);
        public void PushObject(GameObject prefab);
    }

    public class ObjectPool : IObjectPoolUtility
    {
        private Dictionary<string, Queue<GameObject>> objectPool = new();
        // 记录“已经在池内”的实例，避免同一对象被重复 Enqueue。
        private readonly HashSet<int> _pooledInstanceIds = new();
        private GameObject _pool;

        public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            // Debug.Log("VAR");
            GameObject obj;
            // 如果当前出池的对象，不在存在字典对应的队列，或所对应的队列中的预制体个数为0...
            if (!objectPool.ContainsKey(prefab.name) || objectPool[prefab.name].Count == 0)
            {
                obj = GameObject.Instantiate(prefab, position, rotation);  // 则创建新的物体
                PushObject(obj);  // 先预热入池，再立即出池复用，统一对象生命周期路径

                if (_pool == null)  // 如果pool这个代表对象池的物体不存在，则创建一个新的
                {
                    _pool = new GameObject("GameObjectPool");
                }
                
                // 从对象池中，查找子对象池
                GameObject childPool = GameObject.Find(prefab.name + "Pool");
                if (!childPool)  // 如果所查找的子对象池为空
                {
                    // 则创建对应的子对象池，并将其设为对象池的子物体
                    childPool = new GameObject(prefab.name + "Pool");
                    childPool.transform.SetParent(_pool.transform);
                }
                obj.transform.SetParent(childPool.transform);
                
            }
            obj = objectPool[prefab.name].Dequeue(); 
            // 对象已出池，移除“池内标记”，后续才能正常再次回收。
            _pooledInstanceIds.Remove(obj.GetInstanceID());

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);  // 设置为启用状态

            return obj;
        }

        public void PushObject(GameObject prefab)
        {
            if (prefab == null) return;
            int instanceId = prefab.GetInstanceID();
            // 防止重复回收：同一实例二次入池会导致“飞行中被旧回调回收”等问题。
            if (_pooledInstanceIds.Contains(instanceId))
            {
                return;
            }

            // 将生成的预制体的Clone后缀删除
            string name = prefab.name.Replace("(Clone)", string.Empty);
            
            // 若不存在对应的子对象池，则添加新的队列
            if (!objectPool.ContainsKey(name))  
            {
                objectPool.Add(name, new Queue<GameObject>());
            }
            
            // 依据名字将对象从对应队列中入队
            objectPool[name].Enqueue(prefab);
            _pooledInstanceIds.Add(instanceId);
            prefab.SetActive(value: false);
        }
    }
}
