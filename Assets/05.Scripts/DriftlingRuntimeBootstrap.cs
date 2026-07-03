using UnityEngine;

public static class DriftlingRuntimeBootstrap
{
    private const string MapLayerName = "WobbleMap";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreatePrototypeIfNeeded()
    {
        if (Object.FindAnyObjectByType<WobblePlayerController>() != null)
        {
            return;
        }

        if (Object.FindAnyObjectByType<SampleScenePlayerSpawner>() != null)
        {
            return;
        }

        Material white = RuntimeMaterial("Runtime_Driftling_White", new Color(0.93f, 0.95f, 0.96f), 0.34f);
        Material glove = RuntimeMaterial("Runtime_Driftling_Glove", new Color(0.13f, 0.23f, 0.32f), 0.26f);
        Material floor = RuntimeMaterial("Runtime_Workshop_Floor", new Color(0.78f, 0.82f, 0.76f), 0.2f);
        Material edge = RuntimeMaterial("Runtime_Workshop_Edge", new Color(0.26f, 0.34f, 0.38f), 0.15f);
        Material crate = RuntimeMaterial("Runtime_Workshop_CrateBlue", new Color(0.25f, 0.55f, 0.96f), 0.18f);
        Material plate = RuntimeMaterial("Runtime_Workshop_PlateYellow", new Color(0.95f, 0.78f, 0.24f), 0.18f);
        Material goal = RuntimeMaterial("Runtime_Workshop_GoalGreen", new Color(0.23f, 0.9f, 0.55f), 0.45f);
        Material danger = RuntimeMaterial("Runtime_Workshop_DangerCoral", new Color(0.95f, 0.34f, 0.28f), 0.2f);
        Material beam = RuntimeMaterial("Runtime_Workshop_Beam", new Color(0.72f, 0.46f, 0.25f), 0.18f);
        Material glass = RuntimeMaterial("Runtime_Workshop_Glass", new Color(0.55f, 0.78f, 0.9f, 0.55f), 0.75f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.72f, 0.84f, 0.95f);
        RenderSettings.ambientEquatorColor = new Color(0.62f, 0.68f, 0.7f);
        RenderSettings.ambientGroundColor = new Color(0.37f, 0.42f, 0.38f);

        GameObject world = new GameObject("Driftling Workshop Runtime");
        EnsureLighting();

        WobblePlayerController player = CreatePlayer(white, glove);
        Camera camera = EnsureCamera();
        player.cameraTransform = camera.transform;

        BuildLevel(world.transform, floor, edge, crate, plate, goal, danger, beam, glass);

        GameObject hud = new GameObject("Game HUD");
        hud.AddComponent<GameHud>();
    }

    private static void BuildLevel(Transform parent, Material floor, Material edge, Material crate, Material plateMaterial, Material goal, Material danger, Material beamMaterial, Material glass)
    {
        Cube("Start Island", new Vector3(0f, 0f, 0f), new Vector3(12f, 1f, 12f), floor, parent, false, 0f);
        Cube("Gate Island", new Vector3(0f, 0f, 14f), new Vector3(13f, 1f, 13f), floor, parent, false, 0f);
        Cube("Climb Island", new Vector3(0f, 0f, 28f), new Vector3(12f, 1f, 12f), floor, parent, false, 0f);
        Cube("Goal Island", new Vector3(0f, 0f, 43f), new Vector3(13f, 1f, 13f), floor, parent, false, 0f);

        Cube("Low Guard Rail A", new Vector3(-6.2f, 0.65f, 14f), new Vector3(0.25f, 1f, 12f), edge, parent, true, 0f);
        Cube("Low Guard Rail B", new Vector3(6.2f, 0.65f, 14f), new Vector3(0.25f, 1f, 12f), edge, parent, true, 0f);

        Cube("Starter Grab Wall", new Vector3(3.8f, 1.15f, 3f), new Vector3(0.45f, 2.3f, 3.4f), edge, parent, true, 0f);
        Cube("Starter Step", new Vector3(2.2f, 0.62f, 3f), new Vector3(1.8f, 0.35f, 3f), floor, parent, true, 0f);

        GameObject plank = Cube("Loose Plank Bridge", new Vector3(0f, 1.05f, 7.2f), new Vector3(2.6f, 0.28f, 7.5f), beamMaterial, parent, true, 2.6f);
        plank.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;

        GameObject blueCrate = Cube("Blue Weight Crate", new Vector3(-3.4f, 1.1f, 12.3f), new Vector3(1.25f, 1.25f, 1.25f), crate, parent, true, 2.2f);
        blueCrate.GetComponent<Rigidbody>().linearDamping = 0.25f;

        GameObject rollingBall = Sphere("Rolling Counterweight", new Vector3(3.8f, 1.25f, 13.5f), new Vector3(1.1f, 1.1f, 1.1f), crate, parent, true, 1.6f);
        rollingBall.GetComponent<Rigidbody>().linearDamping = 0.15f;

        GameObject door = Cube("Lift Gate", new Vector3(0f, 1.85f, 20.7f), new Vector3(5f, 3.7f, 0.45f), danger, parent, true, 0f);
        GameObject plate = Cube("Pressure Plate", new Vector3(-3f, 0.62f, 16.8f), new Vector3(2.3f, 0.18f, 2.3f), plateMaterial, parent, false, 0f);
        BoxCollider plateCollider = plate.GetComponent<BoxCollider>();
        plateCollider.isTrigger = true;
        plateCollider.size = new Vector3(1f, 2.4f, 1f);
        plateCollider.center = new Vector3(0f, 0.95f, 0f);

        PressurePlate pressurePlate = plate.AddComponent<PressurePlate>();
        pressurePlate.controlledObject = door.transform;
        pressurePlate.closedWorldPosition = door.transform.position;
        pressurePlate.openWorldPosition = door.transform.position + Vector3.up * 4.1f;
        pressurePlate.requiredMass = 1.5f;
        pressurePlate.indicatorRenderer = plate.GetComponent<Renderer>();

        GameObject ramp = Cube("Post Gate Ramp", new Vector3(0f, 0.6f, 24.2f), new Vector3(5.5f, 0.28f, 5f), floor, parent, true, 0f);
        ramp.transform.rotation = Quaternion.Euler(9f, 0f, 0f);

        Cube("Climb Wall", new Vector3(0f, 1.8f, 31.5f), new Vector3(5.8f, 3.6f, 0.5f), edge, parent, true, 0f);
        Cube("Left Ledge", new Vector3(-2.6f, 2.2f, 30.9f), new Vector3(1.5f, 0.28f, 0.9f), beamMaterial, parent, true, 0f);
        Cube("Right Ledge", new Vector3(2.6f, 2.9f, 30.9f), new Vector3(1.5f, 0.28f, 0.9f), beamMaterial, parent, true, 0f);
        Cube("Top Ledge", new Vector3(0f, 3.75f, 31.05f), new Vector3(5.8f, 0.35f, 1.2f), floor, parent, true, 0f);

        GameObject rotatingBeam = Cube("Pivot Beam", new Vector3(0f, 2.4f, 36.5f), new Vector3(7f, 0.32f, 0.32f), beamMaterial, parent, true, 3f);
        HingeJoint hinge = rotatingBeam.AddComponent<HingeJoint>();
        hinge.useLimits = false;
        hinge.axis = Vector3.forward;
        rotatingBeam.GetComponent<Rigidbody>().angularDamping = 0.35f;
        Cube("Pivot Stand", new Vector3(0f, 1.25f, 36.5f), new Vector3(0.4f, 2.4f, 0.4f), edge, parent, true, 0f);

        Cube("Final Glass Bridge", new Vector3(0f, 1.05f, 39.8f), new Vector3(2.2f, 0.24f, 6.2f), glass, parent, true, 0f);

        GameObject goalZone = Cube("Goal Trigger", new Vector3(0f, 1.2f, 43.5f), new Vector3(3.2f, 2.4f, 3.2f), goal, parent, false, 0f);
        goalZone.GetComponent<BoxCollider>().isTrigger = true;
        goalZone.AddComponent<GoalZone>();

        AddSign("Move, jump, then grab the wall with LMB/RMB.", new Vector3(-3.3f, 1.1f, -3.2f), parent);
        AddSign("Drag the blue crate onto the plate.", new Vector3(2.6f, 1.1f, 12.2f), parent);
        AddSign("Grab high, look down, and push forward.", new Vector3(-3.7f, 1.4f, 27.2f), parent);
    }

    private static WobblePlayerController CreatePlayer(Material bodyMaterial, Material handMaterial)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Driftling Player";
        player.transform.position = new Vector3(0f, 2.2f, -4f);
        player.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

        Rigidbody playerBody = player.AddComponent<Rigidbody>();
        playerBody.mass = 7f;
        playerBody.linearDamping = 0.18f;
        playerBody.angularDamping = 4.8f;
        playerBody.interpolation = RigidbodyInterpolation.Interpolate;
        playerBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        WobblePlayerController controller = player.AddComponent<WobblePlayerController>();
        controller.body = playerBody;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Look Head";
        head.transform.SetParent(player.transform);
        head.transform.localPosition = new Vector3(0f, 0.92f, 0.08f);
        head.transform.localScale = new Vector3(0.62f, 0.62f, 0.62f);
        head.GetComponent<Renderer>().sharedMaterial = bodyMaterial;
        Object.Destroy(head.GetComponent<Collider>());
        controller.head = head.transform;

        WobbleHand left = CreateHand("Left Hand", playerBody, controller.leftShoulderLocal, -1f, handMaterial);
        WobbleHand right = CreateHand("Right Hand", playerBody, controller.rightShoulderLocal, 1f, handMaterial);
        controller.leftHand = left;
        controller.rightHand = right;

        CreateArmLine("Left Arm Line", playerBody, left.transform, controller.leftShoulderLocal, handMaterial);
        CreateArmLine("Right Arm Line", playerBody, right.transform, controller.rightShoulderLocal, handMaterial);

        FallRespawn respawn = player.AddComponent<FallRespawn>();
        respawn.controller = controller;
        respawn.minY = -18f;

        return controller;
    }

    private static WobbleHand CreateHand(string name, Rigidbody playerBody, Vector3 shoulderLocal, float side, Material material)
    {
        GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hand.name = name;
        hand.transform.position = playerBody.transform.TransformPoint(shoulderLocal + new Vector3(side * 0.18f, -0.45f, 0.38f));
        hand.transform.localScale = new Vector3(0.34f, 0.34f, 0.34f);
        hand.GetComponent<Renderer>().sharedMaterial = material;

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

    private static void CreateArmLine(string name, Rigidbody body, Transform hand, Vector3 shoulderLocal, Material material)
    {
        GameObject arm = new GameObject(name);
        LineRenderer line = arm.AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        ArmLineRenderer renderer = arm.AddComponent<ArmLineRenderer>();
        renderer.body = body;
        renderer.hand = hand;
        renderer.shoulderLocal = shoulderLocal;
    }

    private static Camera EnsureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.transform.position = new Vector3(0f, 4f, -10f);
        camera.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        camera.fieldOfView = 63f;
        camera.nearClipPlane = 0.08f;
        camera.farClipPlane = 500f;
        return camera;
    }

    private static void EnsureLighting()
    {
        if (Object.FindAnyObjectByType<Light>() != null)
        {
            return;
        }

        GameObject sun = new GameObject("Sun");
        sun.transform.rotation = Quaternion.Euler(45f, -34f, 0f);
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 2.15f;
        light.color = new Color(1f, 0.96f, 0.88f);
    }

    private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool grabbable, float mass)
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.name = name;
        gameObject.transform.SetParent(parent);
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        ApplyMapLayer(gameObject);
        gameObject.GetComponent<Renderer>().sharedMaterial = material;
        AddPhysics(gameObject, grabbable, mass);
        return gameObject;
    }

    private static GameObject Sphere(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool grabbable, float mass)
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gameObject.name = name;
        gameObject.transform.SetParent(parent);
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        ApplyMapLayer(gameObject);
        gameObject.GetComponent<Renderer>().sharedMaterial = material;
        AddPhysics(gameObject, grabbable, mass);
        return gameObject;
    }

    private static void AddPhysics(GameObject gameObject, bool grabbable, float mass)
    {
        if (grabbable)
        {
            gameObject.AddComponent<Grabbable>();
        }

        if (mass <= 0f)
        {
            return;
        }

        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = mass;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private static void ApplyMapLayer(GameObject gameObject)
    {
        int mapLayer = LayerMask.NameToLayer(MapLayerName);
        if (mapLayer >= 0)
        {
            gameObject.layer = mapLayer;
        }
    }

    private static void AddSign(string text, Vector3 position, Transform parent)
    {
        GameObject sign = new GameObject("Hint Sign");
        sign.transform.SetParent(parent);
        sign.transform.position = position;
        TextMesh mesh = sign.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.fontSize = 42;
        mesh.characterSize = 0.055f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = new Color(0.12f, 0.18f, 0.22f);
        sign.AddComponent<SimpleBillboard>();
    }

    private static Material RuntimeMaterial(string name, Color color, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.name = name;
        material.color = color;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        return material;
    }
}
