using System;
using System.Collections;
using QFramework;
using QFramework.UtilityKit;
using DG.Tweening;
using UnityEngine.UI;
using QFramework.Event;
using UnityEngine;

namespace Framework.ViewController.UI
{
    public class LoadingPage : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _loadingCanvasGroup; // 拖入加载面板的 CanvasGroup 组件
        [SerializeField] private Image _loadingImage;
        [SerializeField] private Image _fadePage;

        void OnEnable()
        {
            TypeEventSystem.Global.Register<LoadingEvent.ProgressTo>(e => UpdateProgress(e.Progress))
                .UnRegisterWhenGameObjectDestroyed(gameObject);
            
            TypeEventSystem.Global.Register<LoadingEvent.FadeIn>(e => FadeIn(e.Duration))
                .UnRegisterWhenGameObjectDestroyed(gameObject);
            TypeEventSystem.Global.Register<LoadingEvent.FadeOut>(e => FadeOut(e.Duration))
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            FadeIn(0);
        }

        void Start()
        {
            
        }

        void Update()
        {
            
        }

        private void UpdateProgress(float targetPercent)
        {
            // 参数1：目标值 (0-1)
            // 参数2：动画持续时间
            _loadingImage.DOFillAmount(targetPercent, 0.1f)
                    .SetEase(Ease.InOutCubic); // 设置一个平滑的开头和结尾
        }

        public void FadeIn(float duration = 0.5f)
        {
            // 确保面板在最上层并可以接收点击
            _loadingCanvasGroup.blocksRaycasts = true; 
            
            // 透明度从当前值变到 1
            _loadingCanvasGroup.DOFade(1f, duration);
        }

        // 淡出：隐藏加载界面
        public void FadeOut(float duration = 0.5f)
        {
            // 透明度从当前值变到 0
            _loadingCanvasGroup.DOFade(0f, duration)
                .OnComplete(() => {
                    // 动画结束后，禁用射线检测，防止挡住场景点击
                    _loadingCanvasGroup.blocksRaycasts = false;
                    // 或者直接隐藏整个物体
                    // gameObject.SetActive(false);
                });
        }
        
    }
}