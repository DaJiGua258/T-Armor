using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using QFramework;
using QFramework.Command;

public class ImageFillTween : MonoBehaviour, IController
{
    public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

    [Header("UI References")]
    public Image targetImage;

    [Header("Tween Settings")]
    public float duration = 1f;
    public Ease easeType = Ease.InOutSine;

    private Tween currentTween;

    // 预缓存 TweenCallback，避免每次 lambda 分配
    private TweenCallback onCompleteCallback;

    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
        //     this.SendCommand<PlayerCommand.Damage>(PlayerCommand.Damage.Instance.Init(10));
        // }
    }


    // void Awake()
    // {
    //     if (targetImage == null)
    //         targetImage = GetComponent<Image>();

    //     targetImage.type = Image.Type.Filled;
    //     targetImage.fillMethod = Image.FillMethod.Radial360;
    //     targetImage.fillAmount = 0f;

    //     // 预创建 Tween 并设置为 AutoKill=false，复用对象
    //     PrepareTweens();

    //     // 缓存 callback，不在热路径中创建 lambda
    //     onCompleteCallback = OnTweenComplete;
    // }

    // // ── 预创建并缓存的 Tween ──────────────────────────────
    // private Tweener fillTweener;

    // void PrepareTweens()
    // {
    //     // 创建一个可复用的 Tweener，SetAutoKill(false) 防止播完被销毁
    //     fillTweener = DOTween.To(
    //             getter: () => targetImage.fillAmount,
    //             setter: v => targetImage.fillAmount = v,
    //             endValue: 1f,
    //             duration: duration)
    //         .SetEase(easeType)
    //         .SetAutoKill(false)   // 关键：禁止自动销毁，允许复用
    //         .SetRecyclable(true)  // 放入对象池复用内存
    //         .OnComplete(onCompleteCallback)
    //         .Pause();             // 初始暂停，等待触发
    // }

    // // ── 复用 Tweener 而不是每次 new ───────────────────────
    // void TweenFillTo(float targetFill)
    // {
    //     fillTweener
    //         .ChangeEndValue(targetFill, duration, true) // true = 从当前值开始，0GC
    //         .Restart();
    // }

    // void TweenFillLoop()
    // {
    //     // 循环模式单独处理，Restart 会从头播
    //     fillTweener
    //         .ChangeEndValue(1f, duration, true)
    //         .SetLoops(-1, LoopType.Yoyo)
    //         .Restart();
    // }

    // void StopLoop()
    // {
    //     // 停止循环，恢复单次播放
    //     fillTweener.SetLoops(1);
    //     fillTweener.Pause();
    // }

    // // ── 输入检测（避免字符串/装箱 GC）────────────────────
    // void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.Space)) TweenFillTo(1f);
    //     else if (Input.GetKeyDown(KeyCode.R)) TweenFillTo(0f);
    //     else if (Input.GetKeyDown(KeyCode.H)) TweenFillTo(0.5f);
    //     else if (Input.GetKeyDown(KeyCode.L)) TweenFillLoop();
    //     else if (Input.GetKeyDown(KeyCode.K)) StopLoop();
    //     else if (Input.GetKeyDown(KeyCode.P))
    //     {
    //         if (fillTweener.IsPlaying()) fillTweener.Pause();
    //         else fillTweener.Play();
    //     }
    // }

    // void OnTweenComplete()
    // {
    //     // 预缓存的 callback，无 lambda，无 GC
    //     Debug.Log("Fill 完成");
    // }

    // void OnDestroy()
    // {
    //     // SetAutoKill=false 时必须手动 Kill
    //     fillTweener?.Kill();
    // }

    // // ── OnGUI 用静态字符串避免字符串拼接 GC ──────────────
    // private static readonly string[] GuideLines =
    // {
    //     "=== ImageFill DOTween 0GC 示例 ===",
    //     "[Space] Fill → 1.0",
    //     "[R]     Fill → 0.0",
    //     "[H]     Fill → 0.5",
    //     "[L]     循环播放",
    //     "[P]     暂停 / 继续",
    //     "[K]     停止循环",
    // };

    // // 复用 Rect，避免每帧 struct 装箱
    // private readonly Rect guiArea = new Rect(10, 10, 300, 220);

    // void OnGUI()
    // {
    //     GUILayout.BeginArea(guiArea);
    //     for (int i = 0; i < GuideLines.Length; i++)
    //         GUILayout.Label(GuideLines[i]);
    //     // fillAmount 是 float，直接 ToString("F2") 仍有小 GC
    //     // 若追求极致可用 stackalloc + Span，这里保持可读性
    //     GUILayout.Label($"fillAmount: {targetImage.fillAmount:F2}");
    //     GUILayout.EndArea();
    // }
}