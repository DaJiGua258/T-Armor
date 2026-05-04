using UnityEngine;

namespace QFramework.Utility
{
    public interface IDebugUtility : IUtility
    {
        // public void Log(object obj);
        // public void LogWarning(object obj);
        // public void LogError(object obj);
    }

    public class DebugUtility : IDebugUtility
    {
        public void Log(object obj)
        {
            Debug.Log(obj);
        }

        public void LogError(object obj)
        {
            Debug.LogError(obj);
        }

        public void LogWarning(object obj)
        {
            Debug.LogWarning(obj);
        }
    }
}