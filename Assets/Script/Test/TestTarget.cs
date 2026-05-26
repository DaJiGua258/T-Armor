using UnityEngine;

namespace QFramework.UtilityKit
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class TestTarget : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _moveRange = 8f;

        private Rigidbody2D _rb;
        private Vector2 _origin;
        private int _dir = 1;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _origin = transform.position;
        }

        private void FixedUpdate()
        {
            if (transform.position.x > _origin.x + _moveRange)
                _dir = -1;
            else if (transform.position.x < _origin.x - _moveRange)
                _dir = 1;

            _rb.velocity = new Vector2(_dir * _moveSpeed, 0f);
        }

        private void OnDrawGizmos()
        {
            Vector2 origin = Application.isPlaying ? _origin : (Vector2)transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(origin.x - _moveRange, -10f, 0f),
                            new Vector3(origin.x - _moveRange, 10f, 0f));
            Gizmos.DrawLine(new Vector3(origin.x + _moveRange, -10f, 0f),
                            new Vector3(origin.x + _moveRange, 10f, 0f));
        }
    }
}
