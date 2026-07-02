using System.Collections.Generic;
using DG.Tweening;
using QFramework.Event;
using QFramework.Model;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    public class PickupMessagePanel : BaseUIComponent
    {
        [Header("Prefab & Pool")]
        [SerializeField] private GameObject _itemPrefab;
        [SerializeField] private int _preCreateCount = 5;
        [SerializeField] private int _maxVisible = 5;

        [Header("Layout")]
        [SerializeField] private float _itemHeight = 64f;
        [SerializeField] private float _spacing = 4f;
        [SerializeField] private float _shiftDuration = 0.3f;

        [Header("Animation")]
        [SerializeField] private float _slideDuration = 0.4f;
        [SerializeField] private float _displayDuration = 2f;
        [SerializeField] private float _fadeOutDuration = 0.3f;

        private readonly Stack<PickupMessageItem> _pool = new();
        private readonly List<PickupMessageItem> _activeItems = new();
        private RectTransform _containerRect;
        private float _slideFromX;

        private void Start()
        {
            _containerRect = GetComponent<RectTransform>();

            // 计算滑入起始 X（容器宽度 + item 宽度 = 从屏幕外右侧滑入）
            _slideFromX = _containerRect.rect.width + _itemHeight;

            if (_itemPrefab == null)
            {
                Debug.LogError("[PickupMessagePanel] _itemPrefab 未赋值");
                return;
            }

            for (int i = 0; i < _preCreateCount; i++)
                CreateAndPool();

            TypeEventSystem.Global.Register<StatsEvent.OnItemCollected>(OnItemCollected)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void OnDestroy()
        {
            _pool.Clear();
            _activeItems.Clear();
        }

        // ── 对象池 ──

        private PickupMessageItem CreateAndPool()
        {
            var go = Instantiate(_itemPrefab, transform);
            var item = go.GetComponent<PickupMessageItem>();
            item.OnAnimationComplete = OnItemAnimationComplete;
            item.ResetState();
            _pool.Push(item);
            return item;
        }

        private PickupMessageItem Get()
        {
            while (_pool.Count > 0)
            {
                var item = _pool.Pop();
                if (item != null && item.IsAvailable)
                    return item;
            }
            return CreateAndPool();
        }

        private void Pool(PickupMessageItem item)
        {
            item.ResetState();
            _pool.Push(item);
        }

        // ── 事件处理 ──

        private void OnItemCollected(StatsEvent.OnItemCollected evt)
        {
            // 加载图标
            var config = this.GetModel<IItemConfigModel>().GetItemConfig(evt.ItemType);
            Sprite icon = null;
            if (config != null && !string.IsNullOrEmpty(config.iconPath))
                icon = Resources.Load<Sprite>(config.iconPath);

            var item = Get();
            item.transform.SetAsLastSibling();

            // 放置到最新位置（最底部）
            float startY = _spacing;
            var pos = item.RectTransform.anchoredPosition;
            pos.x = _slideFromX;
            pos.y = startY;
            item.RectTransform.anchoredPosition = pos;

            _activeItems.Add(item);

            // 已有 items 全部上移
            RefreshPositions(0, _activeItems.Count - 1);

            // 新 item 播放入场
            item.Show(icon, evt.ItemName, _slideFromX, _slideDuration, _displayDuration, _fadeOutDuration);

            // 超出最大数量 → 强制移除最旧的
            if (_activeItems.Count > _maxVisible)
                ForceRemoveOldest();
        }

        private void OnItemAnimationComplete(PickupMessageItem item)
        {
            int index = _activeItems.IndexOf(item);
            if (index < 0) return;

            _activeItems.RemoveAt(index);
            Pool(item);

            // 后面的 items 补位
            RefreshPositions(index, _activeItems.Count);
        }

        private void ForceRemoveOldest()
        {
            if (_activeItems.Count <= _maxVisible) return;
            var oldest = _activeItems[0];
            oldest.OnAnimationComplete = null; // 不触发回调，手动处理
            oldest.ResetState();
            _activeItems.RemoveAt(0);
            Pool(oldest);

            RefreshPositions(0, _activeItems.Count);
        }

        // ── 布局 ──

        /// <summary>
        /// 将从 startIndex 到 endIndex（不含 endIndex）的 items 重新排列 Y 位置。
        /// </summary>
        private void RefreshPositions(int startIndex, int endIndex)
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                float targetY = _spacing + (_itemHeight + _spacing) * (_activeItems.Count - 1 - i);
                _activeItems[i].RectTransform
                    .DOAnchorPosY(targetY, _shiftDuration)
                    .SetEase(Ease.OutCubic);
            }
        }
    }
}
