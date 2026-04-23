using UnityEngine;
using System.Collections.Generic;
using System.Collections;


namespace QFramework.ViewController
{
    [RequireComponent(typeof(LineRenderer))]
    public class ElecShock : MonoBehaviour
    {
        public Transform Target;      // 目标
        public int Segments = 10;     // 闪电的分段数
        public float Jitter = 0.5f;   // 抖动幅度
        public float Duration = 0.1f;

        [SerializeField] private ParticleSystem _start;
        [SerializeField] private ParticleSystem _end;

        private LineRenderer _lineRenderer;
        private float _timer;

        void Start()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            
        }

        void Update()
        {
            
        }

        public void DrawElec()
        {
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

                // 如果不是起点和终点，则添加随机偏移
                if (i > 0 && i < Segments)
                {
                    pos += Random.insideUnitSphere * Jitter;
                }

                _lineRenderer.SetPosition(i, pos);
            }
        }

        public IEnumerator StartDraw()
        {
            // float elapsed = 0f;

            // while (elapsed < Duration)
            // {
            //     elapsed += Time.deltaTime;

            //     DrawLightning();

            //     yield return null; // 等待下一帧，直到达到 0.1s
            // }

            DrawElec();

            yield return new WaitForSeconds(0.1f);

            _lineRenderer.positionCount = 0;
        }

        public void Draw(Transform target)
        {
            Target = target;
            StartCoroutine(StartDraw());
        }
    }
}