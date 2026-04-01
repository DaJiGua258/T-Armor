using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QFramework.Utility
{
    public interface IStorageUtility : IUtility
    {
        public void SaveData(string key, object data);
        public object LoadData(string key);
    }
}