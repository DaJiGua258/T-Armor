using UnityEngine;

namespace QFramework.ViewController.Player
{
    public class LegController : MonoBehaviour
    {

        [Header("腿的Transform")]
        public Transform LegFl; // 前左
        public Transform LegFr; // 前右
        public Transform LegBr; // 后右
        public Transform LegBl; // 后左

        [Header("步态参数")]
        public float stepThreshold = 0.4f;    // 触发迈步距离
        public float stepSpeed = 10f;          // 迈步速度
        public float predictionFactor = 0.25f; // 速度预判系数

        [Header("腿的静止偏移（本地坐标）")]
        public Vector2 offsetFl = new Vector2(-0.3f,  0.4f);
        public Vector2 offsetFr = new Vector2( 0.3f,  0.4f);
        public Vector2 offsetBr = new Vector2( 0.3f, -0.4f);
        public Vector2 offsetBl = new Vector2(-0.3f, -0.4f);

        // 内部状态
        [SerializeField] private Rigidbody2D rb;

        private Vector2[] currentPositions = new Vector2[4]; // 腿当前世界坐标
        private Vector2[] targetPositions  = new Vector2[4]; // 腿目标世界坐标
        private Vector2[] stepStartPos     = new Vector2[4]; // 迈步起始位置
        private float[]   stepProgress     = new float[4];   // 迈步进度 0~1
        private bool[]    isStepping       = new bool[4];    // 是否正在迈步

        // 索引定义
        // 0: FL, 1: FR, 2: BR, 3: BL
        // 对角组A: FL(0) + BR(2)
        // 对角组B: FR(1) + BL(3)

        private Transform[] legs;
        private Vector2[]   restOffsets;

        void Start()
        {

            legs = new Transform[] { LegFl, LegFr, LegBr, LegBl };
            restOffsets = new Vector2[] { offsetFl, offsetFr, offsetBr, offsetBl };

            // 初始化：腿直接放到静止位置
            for (int i = 0; i < 4; i++)
            {
                Vector2 worldRest = GetWorldRestPosition(i);
                currentPositions[i] = worldRest;
                targetPositions[i]  = worldRest;
                legs[i].position    = (Vector3)worldRest;
            }
        }

        void UpdateLegPosition()
        {
            Vector2 velocity = rb.velocity;
            bool groupAStepping = isStepping[0] || isStepping[2]; // FL or BR
            bool groupBStepping = isStepping[1] || isStepping[3]; // FR or BL

            // ── 1. 检测是否需要迈步 ──────────────────────────────
            TryStep(0, 2, groupBStepping, velocity); // 组A：FL + BR，对方组B没在迈才能迈
            TryStep(2, 0, groupBStepping, velocity);
            TryStep(1, 3, groupAStepping, velocity); // 组B：FR + BL
            TryStep(3, 1, groupAStepping, velocity);

            // ── 2. 更新迈步进度，推进插值 ────────────────────────
            for (int i = 0; i < 4; i++)
            {
                if (isStepping[i])
                {
                    stepProgress[i] += Time.deltaTime * stepSpeed;

                    if (stepProgress[i] >= 1f)
                    {
                        stepProgress[i]  = 1f;
                        isStepping[i]    = false;
                        currentPositions[i] = targetPositions[i];
                    }
                    else
                    {
                        // Smoothstep 让迈步有缓入缓出
                        float t = SmoothStep(stepProgress[i]);
                        currentPositions[i] = Vector2.Lerp(stepStartPos[i], targetPositions[i], t);
                    }
                }

                legs[i].position = (Vector3)currentPositions[i];
            }
        }

        void Update()
        {
            UpdateLegPosition();
        }

        // ── 辅助方法 ──────────────────────────────────────────────

        /// <summary>
        /// 尝试让 index 这条腿迈步，partner 是同组另一条腿的索引（用于同组同步），
        /// otherGroupStepping 表示另一对角组是否在迈步中
        /// </summary>
        void TryStep(int index, int partner, bool otherGroupStepping, Vector2 velocity)
        {
            if (isStepping[index]) return;           // 自己已在迈步
            if (otherGroupStepping) return;           // 对方组在迈步，等待

            Vector2 worldRest = GetWorldRestPosition(index);
            float dist = Vector2.Distance(currentPositions[index], worldRest);

            if (dist > stepThreshold)
            {
                StartStep(index, velocity);

                // 同组的另一条腿同时迈步（trot对角同步）
                if (!isStepping[partner])
                    StartStep(partner, velocity);
            }
        }

        void StartStep(int index, Vector2 velocity)
        {
            isStepping[index]   = true;
            stepProgress[index] = 0f;
            stepStartPos[index] = currentPositions[index];

            // 目标 = 静止位置 + 速度预判
            targetPositions[index] = GetWorldRestPosition(index) + velocity * predictionFactor;
        }

        /// <summary>
        /// 把本地偏移转换成世界坐标的静止落点
        /// </summary>
        Vector2 GetWorldRestPosition(int index)
        {
            return (Vector2)transform.position
                + (Vector2)transform.TransformDirection(restOffsets[index]);
        }

        float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}