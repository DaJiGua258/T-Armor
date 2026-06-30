using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class JsonArray<T>
{
    public List<T> Items;
}

public static class ConfigLoader
{
    /// <summary>
    /// 从 Resources/{resourcePath} 加载 JSON 配置文件
    /// </summary>
    public static List<T> LoadFromJson<T>(string resourcePath) where T : class
    {
        var textAsset = Resources.Load<TextAsset>(resourcePath);
        if (textAsset == null)
        {
            Debug.LogError($"[ConfigLoader] 配置文件不存在: {resourcePath}");
            return new List<T>();
        }

        var wrapper = JsonUtility.FromJson<JsonArray<T>>(textAsset.text);
        if (wrapper?.Items == null)
        {
            Debug.LogError($"[ConfigLoader] JSON 解析失败: {resourcePath}");
            return new List<T>();
        }

        return wrapper.Items;
    }
}
