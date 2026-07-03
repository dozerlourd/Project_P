using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class OscillatingPlatform : MonoBehaviour
{
    public Vector3 localOffset = new Vector3(0f, 0f, 4f);
    public float period = 4f;
    public float phase;

    private Rigidbody body;
    private Vector3 startPosition;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        startPosition = transform.position;
    }

    private void FixedUpdate()
    {
        float safePeriod = Mathf.Max(0.1f, period);
        float t = Mathf.Sin((Time.time + phase) * Mathf.PI * 2f / safePeriod) * 0.5f + 0.5f;
        body.MovePosition(startPosition + localOffset * t);
    }
}
