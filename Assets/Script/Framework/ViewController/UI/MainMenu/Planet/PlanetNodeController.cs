using QFramework;
using QFramework.Command;
using QFramework.Manager;
using QFramework.ViewController.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlanetNodeController : AbstractBasePanel, IPointerEnterHandler, IPointerExitHandler
{
    private Transform planet;
    private Transform mainCam;
    private CanvasGroup _canvasGroup;
    public PlanetNodeMapData MapData { get; private set; }

    private const float NORMAL_ALPHA = 0.5f;
    private const float HIGHLIGHT_ALPHA = 1f;

    [Header("缩放设置")]
    public float baseScale = 0.01f;     // World Space UI 基础大小
    public float minScaleLimit = 0.4f;  // 在边缘时的最小比例
    public float maxScaleLimit = 1.0f;  // 正对时的最大比例

    private void Awake()
    {
        // 确保有 CanvasGroup 统一控制透明度
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 将所有子 Image 的 alpha 置为 1，由 CanvasGroup 整体控制
        var images = GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            Color c = img.color;
            c.a = 1f;
            img.color = c;
        }

        // 禁用 Button 的 ColorTint，避免与 CanvasGroup 冲突
        var button = GetComponent<Button>();
        if (button != null)
            button.transition = Selectable.Transition.None;

        _canvasGroup.alpha = NORMAL_ALPHA;
    }

    public void Init(Transform planetTransform, PlanetNodeMapData mapData)
    {
        // 注册点击事件监听
        gameObject.GetComponent<Button>().onClick
            .AddListener(() =>
            {
                _canvasGroup.alpha = HIGHLIGHT_ALPHA;
                MainUIManager.Instance.EnterLevelConfirm(transform.position);
                this.SendCommand<MainMenuCommand.SelectLevel>(new MainMenuCommand.SelectLevel(mapData));
            });

        // 初始化数据
        planet = planetTransform;
        mainCam = Camera.main.transform;
        MapData = mapData;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _canvasGroup.alpha = HIGHLIGHT_ALPHA;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _canvasGroup.alpha = NORMAL_ALPHA;
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
        Vector3 dirToCam = mainCam.position - transform.position;
        if (dirToCam != Vector3.zero)
        {
            transform.LookAt(mainCam, mainCam.up);
            transform.Rotate(0, 180, 0);
        }

        // 2. 边缘缩放逻辑
        Vector3 nodeNormal = (transform.position - planet.position).normalized;
        float dot = Mathf.Clamp01(Vector3.Dot(nodeNormal, dirToCam.normalized));
        float scale = Mathf.Lerp(minScaleLimit, maxScaleLimit, dot) * baseScale;
        transform.localScale = new Vector3(scale, scale, scale);
    }
}
