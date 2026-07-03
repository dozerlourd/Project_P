using UnityEngine;

public sealed class SampleScenePlayerSpawner : MonoBehaviour
{
    public GameObject playerPrefab;
    public Transform spawnPoint;
    public Camera sceneCamera;
    public Material handMaterial;
    public float respawnMinY = -35f;

    private void Awake()
    {
        if (Object.FindAnyObjectByType<WobblePlayerController>() != null)
        {
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogWarning("SampleScenePlayerSpawner needs a player prefab.");
            return;
        }

        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
        GameObject player = Instantiate(playerPrefab, position, rotation);
        player.name = playerPrefab.name;

        WobblePlayerController controller = player.GetComponent<WobblePlayerController>();
        Rigidbody body = player.GetComponent<Rigidbody>();
        if (controller == null || body == null)
        {
            Debug.LogWarning("Player prefab must contain WobblePlayerController and Rigidbody.");
            return;
        }

        controller.body = body;
        controller.cameraTransform = sceneCamera != null ? sceneCamera.transform : Camera.main != null ? Camera.main.transform : null;
        EnsureHands(controller, body);

        FallRespawn respawn = player.GetComponent<FallRespawn>();
        if (respawn != null)
        {
            respawn.controller = controller;
            respawn.minY = respawnMinY;
        }
    }

    private void EnsureHands(WobblePlayerController controller, Rigidbody body)
    {
        if (controller.leftHand == null)
        {
            controller.leftHand = CreateHand("Left Hand", body, controller.leftShoulderLocal, -1f);
        }

        if (controller.rightHand == null)
        {
            controller.rightHand = CreateHand("Right Hand", body, controller.rightShoulderLocal, 1f);
        }

        EnsureArmLine("Left Arm Line", body, controller.leftHand.transform, controller.leftShoulderLocal);
        EnsureArmLine("Right Arm Line", body, controller.rightHand.transform, controller.rightShoulderLocal);
    }

    private WobbleHand CreateHand(string objectName, Rigidbody playerBody, Vector3 shoulderLocal, float side)
    {
        GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hand.name = objectName;
        hand.transform.position = playerBody.transform.TransformPoint(shoulderLocal + new Vector3(side * 0.18f, -0.45f, 0.38f));
        hand.transform.localScale = new Vector3(0.34f, 0.34f, 0.34f);

        if (handMaterial != null)
        {
            hand.GetComponent<Renderer>().sharedMaterial = handMaterial;
        }

        Rigidbody handBody = hand.AddComponent<Rigidbody>();
        handBody.mass = 0.45f;
        handBody.linearDamping = 0.18f;
        handBody.angularDamping = 0.45f;
        handBody.interpolation = RigidbodyInterpolation.Interpolate;
        handBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        WobbleHand wobbleHand = hand.AddComponent<WobbleHand>();
        wobbleHand.Configure(playerBody, shoulderLocal, 2.35f);
        return wobbleHand;
    }

    private void EnsureArmLine(string objectName, Rigidbody body, Transform hand, Vector3 shoulderLocal)
    {
        if (hand == null)
        {
            return;
        }

        GameObject arm = new GameObject(objectName);
        LineRenderer line = arm.AddComponent<LineRenderer>();
        line.sharedMaterial = handMaterial;

        ArmLineRenderer renderer = arm.AddComponent<ArmLineRenderer>();
        renderer.body = body;
        renderer.hand = hand;
        renderer.shoulderLocal = shoulderLocal;
    }
}
