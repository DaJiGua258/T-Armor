using UnityEngine;

public class CircleMovement : MonoBehaviour
{
    [SerializeField] private float radius = 5f;
    [SerializeField] private float speed = 2f;

    private Vector3 _center;
    private float _angle;

    private void Start()
    {
        _center = transform.position;
    }

    private void Update()
    {
        _angle += speed * Time.deltaTime;
        Vector3 offset = new Vector3(Mathf.Cos(_angle), Mathf.Sin(_angle), 0) * radius;
        transform.position = _center + offset;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? _center : transform.position, radius);
    }
}
