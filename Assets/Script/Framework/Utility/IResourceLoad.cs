using UnityEngine;

namespace QFramework.Utility
{
    public interface IResourceLoad : IUtility
    {
        public T Load<T>(string path) where T : Object;
        public T[] LoadAll<T>(string path) where T : Object;
    }

    public class ResouceLoad : IResourceLoad
    {
        public T Load<T>(string path) where T : Object
        {
            // 目前先使用 Resources.Load
            T asset = Resources.Load<T>(path);

            return asset;
        }

        public T[] LoadAll<T>(string path) where T : Object
        {
            return Resources.LoadAll<T>(path);
        }
    }


}