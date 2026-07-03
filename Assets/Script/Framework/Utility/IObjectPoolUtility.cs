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
            // 先清理队列中已被外部销毁的悬空引用
            if (objectPool.TryGetValue(prefab.name, out var queue))
            {
                while (queue.Count > 0 && queue.Peek() == null)
                {
                    queue.Dequeue();
                }
            }

            GameObject obj;
            // 队列为空时直接实例化
            if (queue == null || queue.Count == 0)
            {
                obj = GameObject.Instantiate(prefab, position, rotation);
                PushObject(obj);

                if (_pool == null)
                {
                    _pool = new GameObject("GameObjectPool");
                }

                GameObject childPool = GameObject.Find(prefab.name + "Pool");
                if (!childPool)
                {
                    childPool = new GameObject(prefab.name + "Pool");
                    childPool.transform.SetParent(_pool.transform);
                }
                obj.transform.SetParent(childPool.transform);
            }

            obj = objectPool[prefab.name].Dequeue();
            _pooledInstanceIds.Remove(obj.GetInstanceID());

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);

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
