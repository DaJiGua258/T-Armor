using UnityEngine;

namespace QFramework.ViewController.UI
{
    [RequireComponent(typeof(LineRenderer))]
    public class NodeLine : MonoBehaviour
    {
        [Header("端点世界坐标")]
        public Vector3 fromPoint;
        public Vector3 toPoint;

        [Header("参数")]
        public int segments = 60;

        private LineRenderer lr;
        private Vector3 globeCenter;


        public void Init(Vector3 from, Vector3 to, Vector3 center)
        {
            fromPoint   = from;
            toPoint     = to;
            globeCenter = center;

            lr = GetComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop          = false;
            lr.alignment     = LineAlignment.TransformZ;

            DrawArc();
            UpdateOrientation();
        }

        void Update()
        {
            DrawArc();
        }

        void DrawArc()
        {
            Vector3 dirA = (fromPoint - globeCenter).normalized;
            Vector3 dirB = (toPoint   - globeCenter).normalized;
            float radiusA = Vector3.Distance(fromPoint, globeCenter);
            float radiusB = Vector3.Distance(toPoint,   globeCenter);

            var positions = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float t      = (float)i / segments;
                Vector3 dir  = Vector3.Slerp(dirA, dirB, t);
                float radius = Mathf.Lerp(radiusA, radiusB, t); // 两端半径不同时平滑过渡
                positions[i] = globeCenter + dir * radius;
            }

            lr.positionCount = segments + 1;
            lr.SetPositions(positions);
        }

        void UpdateOrientation()
        {
            // 取弧线中点
            Vector3 midDir = Vector3.Slerp(
                (fromPoint - globeCenter).normalized,
                (toPoint   - globeCenter).normalized,
                0.5f
            );
            Vector3 midPoint = globeCenter + midDir * Vector3.Distance(fromPoint, globeCenter);

            // Z轴 = 球面法线方向（从球心指向中点）
            Vector3 normal = (midPoint - globeCenter).normalized;

            // X轴 = 线条延伸方向
            Vector3 lineDir = (toPoint - fromPoint).normalized;

            // 防止法线和lineDir平行
            if (Mathf.Abs(Vector3.Dot(normal, lineDir)) > 0.99f)
                lineDir = Vector3.up;

            Vector3 up = Vector3.Cross(lineDir, normal).normalized;

            transform.rotation = Quaternion.LookRotation(normal, up);
        }
    }
}