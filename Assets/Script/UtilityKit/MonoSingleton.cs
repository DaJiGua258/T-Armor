using System;
using System.Collections;
using System.Collections.Generic;
using QFramework.Utility;
using UnityEngine;

namespace QFramework.UtilityKit
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T s_instance;

        public static T Instance
        {
            get { return s_instance; }
            set { s_instance = value; }
        }

        protected virtual void Awake()
        {
            if (Instance == null)
            {
                s_instance = (T)this;
            }
            else
            {
                Destroy(gameObject);
            }

            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// 单例模式，用于在Awake中进行实例的替换
    /// </summary>
    public class DesMonoSingleton<T> : MonoBehaviour, IController where T : DesMonoSingleton<T>
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        protected IDebugUtility DebugUtility => this.GetUtility<IDebugUtility>();
        protected IResourceLoad ResourceLoad => this.GetUtility<IResourceLoad>();

        private static T s_instance;
        public static T Instance
        {
            get { return s_instance; }
            set { s_instance = value; }
        }

        protected virtual void Awake()
        {
            if (Instance == null)
            {
                s_instance = (T)this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// 重写单例模式，用于在Awake中进行实例的替换
    /// </summary>
    public class OverrideMonoSingleton<T> : MonoBehaviour, IController where T : OverrideMonoSingleton<T>
    {
        private static T s_instance;

        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        public IDebugUtility DebugUtility => this.GetUtility<IDebugUtility>();
        public IResourceLoad ResourceLoad => this.GetUtility<IResourceLoad>();
        public IInputUtility InputUtility => this.GetUtility<IInputUtility>();
        

        protected virtual void Awake()
        {
            // 如果已经存在一个旧实例，且不是当前这个
            if (s_instance != null && s_instance != (T)this)
            {
                // 记录日志（可选）
                DebugUtility.LogWarning($"{typeof(T).Name} 旧实例已被新实例替换。");

                // 销毁旧的物体
                Destroy(s_instance.gameObject);
            }

            // 将当前（最新的）实例赋值给静态变量
            s_instance = (T)this;
            
            // 如果你希望跨场景保留，可以加上：
            // DontDestroyOnLoad(gameObject);
        }
    }
}