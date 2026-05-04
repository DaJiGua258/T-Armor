using UnityEngine;

namespace QFramework.ViewController
{
    [RequireComponent(typeof(LineRenderer))]
    public class ElecShock : MonoBehaviour
    {
        public Transform Target;      // 目标
        public int Segments = 10;     // 闪电的分段数
        public float Jitter = 0.5f;   // 抖动幅度

        [SerializeField] private ParticleSystem _start;
        [SerializeField] private ParticleSystem _end;

        private LineRenderer _lineRenderer;

        void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
        }

        void Start()
        {
            _lineRenderer.enabled = false;
        }

        void OnDisable()
        {
            if (_lineRenderer != null)
            {
                _lineRenderer.positionCount = 0;
                _lineRenderer.enabled = false;
            }
        }

        public void Draw(Transform target)
        {
            Target = target;
            gameObject.SetActive(true);
            DrawElec();
            Invoke(nameof(ClearLine), 0.1f);
        }

        private void DrawElec()
        {
            if (Target == null) return;

            _lineRenderer.enabled = true;
            _lineRenderer.positionCount = Segments + 1;
            Vector3 startPos = transform.position;
            Vector3 endPos = Target.position;

            _start.transform.position = startPos;
            _end.transform.position = endPos;

            _start.Play();
            _end.Play();

            for (int i = 0; i <= Segments; i++)
            {
                float t = (float)i / Segments;
                Vector3 pos = Vector3.Lerp(startPos, endPos, t);

                if (i > 0 && i < Segments)
                {
                    pos += Random.insideUnitSphere * Jitter;
                }

                _lineRenderer.SetPosition(i, pos);
            }
        }

        private void ClearLine()
        {
            _lineRenderer.positionCount = 0;
            _lineRenderer.enabled = false;
        }
    }
}
