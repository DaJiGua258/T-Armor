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
    private Image[] _nodeImages;
    private bool _isHighlighted;
    private bool _isClicked;
    public PlanetNodeMapData MapData { get; private set; }

    private const float HIGHLIGHT_ALPHA = 1f;
    private static readonly Color DefaultNodeColor = Color.gray;
    private static readonly Color HighlightNodeColor = Color.white;

    [Header("缩放设置")]
    public float baseScale = 0.01f;     // World Space UI 基础大小
    public float minScaleLimit = 0.4f;  // 在边缘时的最小比例
    public float maxScaleLimit = 1.0f;  // 正对时的最大比例

    [Header("透明度设置")]
    public float minAlpha = 0.3f;       // 法线垂直相机时的最小透明度
    public float maxAlpha = 1.0f;       // 法线正对相机时的最大透明度

    private void Awake()
    {
        // 确保有 CanvasGroup 统一控制透明度
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 保存所有子 Image 引用，将颜色设为灰色，alpha 统一为 1 由 CanvasGroup 控制
        _nodeImages = GetComponentsInChildren<Image>(true);
        foreach (var img in _nodeImages)
        {
            img.color = DefaultNodeColor;
        }

        // 禁用 Button 的 ColorTint，避免与 CanvasGroup 冲突
        var button = GetComponent<Button>();
        if (button != null)
            button.transition = Selectable.Transition.None;

        _canvasGroup.alpha = 1f;
    }

    public void Init(Transform planetTransform, PlanetNodeMapData mapData)
    {
        // 注册点击事件监听
        gameObject.GetComponent<Button>().onClick
            .AddListener(() =>
            {
                _isClicked = true;
                _isHighlighted = true;
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
        _isHighlighted = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isClicked)
            _isHighlighted = false;
    }

    public void SetHighlighted(bool highlighted)
    {
        _isClicked = false;
        _isHighlighted = highlighted;
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

        // 3. 透明度与颜色：点击/悬停时白色不透明，否则根据法线与相机夹角动态变化
        if (_isHighlighted || _isClicked)
        {
            _canvasGroup.alpha = HIGHLIGHT_ALPHA;
            foreach (var img in _nodeImages)
                img.color = HighlightNodeColor;
        }
        else
        {
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, dot);
            _canvasGroup.alpha = alpha;
            foreach (var img in _nodeImages)
                img.color = DefaultNodeColor;
        }
    }
}
