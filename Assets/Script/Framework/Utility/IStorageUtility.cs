using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QFramework.Utility
{
    public interface IStorageUtility : IUtility
    {
        void SaveData(string key, object data);
        object LoadData(string key);
        T LoadData<T>(string key) where T : class;
        void DeleteData(string key);
    }
}