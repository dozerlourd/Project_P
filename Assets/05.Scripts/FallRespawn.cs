using UnityEngine;

public sealed class FallRespawn : MonoBehaviour
{
    public WobblePlayerController controller;
    public float minY = -18f;

    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<WobblePlayerController>();
        }
    }

    private void Update()
    {
        if (controller != null && transform.position.y < minY)
        {
            controller.Respawn();
        }
    }
}
