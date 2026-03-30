using UnityEngine;
using UnityEngine.AddressableAssets; // 必须引入
using UnityEngine.ResourceManagement.AsyncOperations; // 异步句柄相关

public class SimpleLoader : MonoBehaviour
{
    // 你在 Inspector 里给资源起的地址名称
    public string resourceAddress = "Walls";

    void Start()
    {
        // 1. 异步加载资源
        Addressables.InstantiateAsync(resourceAddress).Completed += OnLoadCompleted;
    }

    private void OnLoadCompleted(AsyncOperationHandle<GameObject> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("资源加载并实例化成功！");
        }
        else
        {
            Debug.LogError("资源加载失败，请检查地址是否正确。");
        }
    }
}