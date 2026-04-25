using UnityEngine;
using System.Collections.Generic;

public class ProceduralGait : MonoBehaviour
{
    [Header("Leg Transforms")]
    public List<Transform> legTransforms;

    [Header("Layout")]
    public Vector2 baseOffset = new Vector2(0.2f, 0.2f);
    public float legSpacing = 0.3f;
    public float lateralOffset = 0.5f;

    [Header("Step Settings")]
    public float stepThreshold = 0.2f;
    public float stepDuration = 0.15f;
    public float stepHeight = 0.1f;
    [Tooltip("身体速度预判系数：速度越快，落点越靠前")]
    public float velocityPredictionScale = 0.5f;

    // 身体速度（用于预判落脚点）
    private Vector3 _bodyVelocity;
    private Vector3 _lastBodyPos;

    private List<LegState> _legs = new List<LegState>();

    // ── 内部状态 ────────────────────────────────────────────────
    private class LegState
    {
        public Transform tf;
        public int index;
        public bool isLeft;

        public Vector3 currentPos;   // 当前落地世界坐标
        public Vector3 targetPos;    // 目标落地世界坐标
        public Vector3 startPos;     // 本次迈步起点

        public float stepProgress = 1f; // 1 = 落地完成

        public bool IsStepping => stepProgress < 1f;

        public LegState(Transform t, int i, bool left, Vector3 pos)
        {
            tf = t; index = i; isLeft = left;
            currentPos = targetPos = startPos = pos;
        }
    }

    // ── 生命周期 ─────────────────────────────────────────────────
    void Start()
    {
        _lastBodyPos = transform.position;
        _legs.Clear();

        for (int i = 0; i < legTransforms.Count; i++)
        {
            if (legTransforms[i] == null) continue;
            Vector3 startPos = SampleGround(GetRestLocalOffset(i, i % 2 == 0), legTransforms[i].position.z);
            _legs.Add(new LegState(legTransforms[i], i, i % 2 == 0, startPos));
        }
    }

    void Update()
    {
        UpdateBodyVelocity();

        for (int i = 0; i < _legs.Count; i++)
        {
            var leg = _legs[i];

            if (!leg.IsStepping)
            {
                TryInitiateStep(leg, i);
            }

            if (leg.IsStepping)
            {
                AdvanceStep(leg);
            }

            leg.tf.position = leg.currentPos;
        }
    }

    // ── 核心逻辑 ─────────────────────────────────────────────────

    void UpdateBodyVelocity()
    {
        _bodyVelocity = (transform.position - _lastBodyPos) / Time.deltaTime;
        _lastBodyPos = transform.position;
    }

    void TryInitiateStep(LegState leg, int legIndex)
    {
        Vector3 idealPos = SampleGround(GetRestLocalOffset(leg.index, leg.isLeft), leg.tf.position.z);
        float dist = Vector3.Distance(leg.currentPos, idealPos);

        if (dist <= stepThreshold) return;

        // 约束：同侧相邻腿不能同时迈步（防止同步跳动）
        if (HasAdjacentLegStepping(legIndex)) return;

        // 启动迈步
        leg.stepProgress = 0f;
        leg.startPos = leg.currentPos;

        // 落点 = 理想位置 + 身体速度预判
        Vector3 predicted = idealPos + _bodyVelocity * velocityPredictionScale * stepDuration;
        leg.targetPos = SampleGround(predicted, leg.tf.position.z);
    }

    void AdvanceStep(LegState leg)
    {
        leg.stepProgress = Mathf.Clamp01(leg.stepProgress + Time.deltaTime / stepDuration);

        float t = leg.stepProgress;
        float easedT = t * t * (3f - 2f * t);

        // 只插值 XY，Z 保持不变
        Vector3 horizontal = Vector3.Lerp(leg.startPos, leg.targetPos, easedT);
        leg.currentPos = new Vector3(horizontal.x, horizontal.y, leg.currentPos.z);
    }

    /// <summary>
    /// 检查同侧相邻腿是否正在迈步（避免同侧两腿同时离地）
    /// 约定：偶数索引为左腿，奇数为右腿；index 和 index±2 为同组相邻腿
    /// </summary>
    bool HasAdjacentLegStepping(int legIndex)
    {
        // // 检查同侧（步距±2）
        // int[] neighbors = { legIndex - 2, legIndex + 2 };
        // foreach (int n in neighbors)
        // {
        //     if (n >= 0 && n < _legs.Count && _legs[n].IsStepping)
        //         return true;
        // }
        // return false;

        foreach (var leg in _legs)
        {
            if (leg.IsStepping) return true;
        }
        return false;
    }

    // ── 工具方法 ─────────────────────────────────────────────────

    /// <summary>
    /// 计算脚的本地偏移（基于身体局部空间）
    /// </summary>
    Vector3 GetRestLocalOffset(int index, bool isLeft)
    {
        int groupIndex = index / 2;
        float x = isLeft ? -baseOffset.x : baseOffset.x;
        float y = baseOffset.y - groupIndex * legSpacing;

        bool flip = groupIndex % 2 == 1;
        float sign = isLeft ? 1f : -1f;
        if (flip) sign = -sign;
        y += sign * lateralOffset;

        return transform.TransformPoint(new Vector3(x, y, 0));
    }

    /// <summary>
    /// 从给定世界点向下射线检测，返回地面接触点；未命中则返回原点
    /// </summary>
    Vector3 SampleGround(Vector3 worldPoint, float originalZ)
    {
        return new Vector3(worldPoint.x, worldPoint.y, originalZ);
    }

    // ── Gizmos ───────────────────────────────────────────────────
    void OnDrawGizmos()
    {
        if (legTransforms == null) return;

        for (int i = 0; i < legTransforms.Count; i++)
        {
            bool isLeft = i % 2 == 0;
            Vector3 ideal = GetRestLocalOffset(i, isLeft);

            // 理想位置（青色）
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(ideal, 0.02f);

            // 阈值圆（半透明黄）
            Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
            DrawGizmoCircle(ideal, stepThreshold);
        }

        if (!Application.isPlaying) return;

        foreach (var leg in _legs)
        {
            Vector3 ideal = GetRestLocalOffset(leg.index, leg.isLeft);

            // 当前落点（红）+ 连线
            Gizmos.color = leg.IsStepping ? Color.green : Color.red;
            Gizmos.DrawLine(ideal, leg.currentPos);
            Gizmos.DrawWireSphere(leg.currentPos, 0.03f);

            // 目标落点（绿，仅迈步中）
            if (leg.IsStepping)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(leg.targetPos, 0.025f);
            }
        }
    }

    void DrawGizmoCircle(Vector3 center, float radius, int segments = 24)
    {
        float step = 2f * Mathf.PI / segments;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * step;
            Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}