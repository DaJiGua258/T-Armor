using QFramework;
using QFramework.Command;
using QFramework.Manager;
using QFramework.ViewController.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlanetNodeController : AbstractBasePanel
{
    private Transform planet;
    private Transform mainCam;
    public PlanetNodeMapData MapData { get; private set; }

    [Header("缩放设置")]
    public float baseScale = 0.01f;     // World Space UI 基础大小
    public float minScaleLimit = 0.4f;  // 在边缘时的最小比例
    public float maxScaleLimit = 1.0f;  // 正对时的最大比例

    public void Init(Transform planetTransform, PlanetNodeMapData mapData)
    {
        // 注册点击事件监听，点击图标后加载关卡信息
        gameObject.GetComponent<Button>().onClick
            .AddListener(() => 
            {
                MainUIManager.Instance.EnterLevelConfirm();
                this.SendCommand<MainMenuCommand.SelectLevel>(new MainMenuCommand.SelectLevel(mapData));
            });


    
        // 初始化数据
        planet = planetTransform;
        mainCam = Camera.main.transform;
        MapData = mapData;
    }


    void LateUpdate()
    {
        UpdateNodeUI();
    }

    /// <summary>
    /// 更新节点UI显示
    /// </summary>
    private void UpdateNodeUI()
    {
        
        if (planet == null || mainCam == null) return;

        // 1. 广告牌：始终面向摄像机
        // 使用 LookRotation 让 UI 面板正对相机
        Vector3 dirToCam = mainCam.position - transform.position;
        if (dirToCam != Vector3.zero)
        {
            transform.LookAt(mainCam, mainCam.up);
            transform.Rotate(0, 180, 0);
        }

        // 2. 边缘缩放逻辑：增加空间深度的视觉反馈
        Vector3 nodeNormal = (transform.position - planet.position).normalized;
        // 计算点积：1 代表 UI 在星球正中心对着你，0 代表 UI 在星球边缘
        float dot = Mathf.Clamp01(Vector3.Dot(nodeNormal, dirToCam.normalized));
        float scale = Mathf.Lerp(minScaleLimit, maxScaleLimit, dot) * baseScale;
        transform.localScale = new Vector3(scale, scale, scale);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("射线碰到了: " + gameObject.name);
    }
}