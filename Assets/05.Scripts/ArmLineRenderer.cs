using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class ArmLineRenderer : MonoBehaviour
{
    public Rigidbody body;
    public Transform hand;
    public Vector3 shoulderLocal;
    public float width = 0.12f;

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = width;
        line.endWidth = width * 0.82f;
        line.numCornerVertices = 5;
        line.numCapVertices = 5;
    }

    private void LateUpdate()
    {
        if (body == null || hand == null)
        {
            return;
        }

        line.SetPosition(0, body.transform.TransformPoint(shoulderLocal));
        line.SetPosition(1, hand.position);
    }
}
