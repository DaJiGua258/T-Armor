using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T s_instance;

    public static T Instance
    {
        get { return s_instance; }
        set { s_instance = value; }
    }

    public virtual void Awake()
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
