using UnityEngine;

public sealed class SimpleBillboard : MonoBehaviour
{
    private void LateUpdate()
    {
        Camera activeCamera = Camera.main;
        if (activeCamera == null)
        {
            return;
        }

        Vector3 toCamera = transform.position - activeCamera.transform.position;
        if (toCamera.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }
    }
}
