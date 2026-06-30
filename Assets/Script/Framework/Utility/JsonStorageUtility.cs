using System.IO;
using UnityEngine;

namespace QFramework.Utility
{
    public class JsonStorageUtility : IStorageUtility
    {
        private static string GetPath(string key)
        {
            return Path.Combine(Application.persistentDataPath, key + ".json");
        }

        public void SaveData(string key, object data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetPath(key), json);
        }

        public object LoadData(string key)
        {
            string path = GetPath(key);
            if (!File.Exists(path)) return null;
            return File.ReadAllText(path);
        }

        public T LoadData<T>(string key) where T : class
        {
            string json = LoadData(key) as string;
            if (string.IsNullOrEmpty(json)) return null;
            return JsonUtility.FromJson<T>(json);
        }

        public void DeleteData(string key)
        {
            string path = GetPath(key);
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
