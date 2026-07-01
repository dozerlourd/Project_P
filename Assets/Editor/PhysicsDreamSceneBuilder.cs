using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PhysicsDreamSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/WobbleWorkshop.unity";
    private const string ClimbGauntletScenePath = "Assets/Scenes/WobbleClimbGauntlet.unity";
    private const string MapLayerName = "WobbleMap";

    [MenuItem("Tools/Driftling Workshop/Rebuild Prototype Scene")]
    public static void BuildScene()
    {
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets", "Scenes");

        Material white = MaterialAsset("Driftling_White", new Color(0.93f, 0.95f, 0.96f), 0.34f);
        Material glove = MaterialAsset("Driftling_Glove", new Color(0.13f, 0.23f, 0.32f), 0.26f);
        Material floor = MaterialAsset("Workshop_Floor", new Color(0.78f, 0.82f, 0.76f), 0.2f);
        Material edge = MaterialAsset("Workshop_Edge", new Color(0.26f, 0.34f, 0.38f), 0.15f);
        Material crate = MaterialAsset("Workshop_CrateBlue", new Color(0.25f, 0.55f, 0.96f), 0.18f);
        Material plate = MaterialAsset("Workshop_PlateYellow", new Color(0.95f, 0.78f, 0.24f), 0.18f);
        Material goal = MaterialAsset("Workshop_GoalGreen", new Color(0.23f, 0.9f, 0.55f), 0.45f);
        Material danger = MaterialAsset("Workshop_DangerCoral", new Color(0.95f, 0.34f, 0.28f), 0.2f);
        Material beam = MaterialAsset("Workshop_Beam", new Color(0.72f, 0.46f, 0.25f), 0.18f);
        Material glass = MaterialAsset("Workshop_Glass", new Color(0.55f, 0.78f, 0.9f, 0.55f), 0.75f);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.72f, 0.84f, 0.95f);
        RenderSettings.ambientEquatorColor = new Color(0.62f, 0.68f, 0.7f);
        RenderSettings.ambientGroundColor = new Color(0.37f, 0.42f, 0.38f);

        GameObject world = new GameObject("Driftling Workshop");

        CreateLighting();
        WobblePlayerController player = CreatePlayer(white, glove);
        Camera camera = CreateCamera();
        player.cameraTransform = camera.transform;

        BuildLevel(world.transform, floor, edge, crate, plate, goal, danger, beam, glass);

        GameObject hud = new GameObject("Game HUD");
        hud.AddComponent<GameHud>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Driftling Workshop prototype scene rebuilt at " + ScenePath);
    }

    [MenuItem("Tools/Driftling Workshop/Build Climb Gauntlet Scene")]
    public static void BuildClimbGauntletScene()
    {
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets", "Scenes");

        Material white = MaterialAsset("Driftling_White", new Color(0.93f, 0.95f, 0.96f), 0.34f);
        Material glove = MaterialAsset("Driftling_Glove", new Color(0.13f, 0.23f, 0.32f), 0.26f);
        Material floor = MaterialAsset("Workshop_Floor", new Color(0.78f, 0.82f, 0.76f), 0.2f);
        Material edge = MaterialAsset("Workshop_Edge", new Color(0.26f, 0.34f, 0.38f), 0.15f);
        Material crate = MaterialAsset("Workshop_CrateBlue", new Color(0.25f, 0.55f, 0.96f), 0.18f);
        Material plate = MaterialAsset("Workshop_PlateYellow", new Color(0.95f, 0.78f, 0.24f), 0.18f);
        Material goal = MaterialAsset("Workshop_GoalGreen", new Color(0.23f, 0.9f, 0.55f), 0.45f);
        Material danger = MaterialAsset("Workshop_DangerCoral", new Color(0.95f, 0.34f, 0.28f), 0.2f);
        Material beam = MaterialAsset("Workshop_Beam", new Color(0.72f, 0.46f, 0.25f), 0.18f);
        Material glass = MaterialAsset("Workshop_Glass", new Color(0.55f, 0.78f, 0.9f, 0.55f), 0.75f);
        Material climb = MaterialAsset("Workshop_ClimbTeal", new Color(0.15f, 0.68f, 0.63f), 0.22f);
        Material violet = MaterialAsset("Workshop_VioletPad", new Color(0.56f, 0.43f, 0.92f), 0.28f);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.72f, 0.84f, 0.95f);
        RenderSettings.ambientEquatorColor = new Color(0.62f, 0.68f, 0.7f);
        RenderSettings.ambientGroundColor = new Color(0.37f, 0.42f, 0.38f);

        GameObject world = new GameObject("Driftling Climb Gauntlet");

        CreateLighting();
        WobblePlayerController player = CreatePlayer(white, glove, new Vector3(0f, 2.2f, -6f));
        player.cameraDistance = 7.1f;
        player.climbAssist = 46f;
        player.hangClimbRiseSpeed = 1.75f;
        player.GetComponent<FallRespawn>().minY = -28f;

        Camera camera = CreateCamera();
        player.cameraTransform = camera.transform;

        BuildClimbGauntletLevel(world.transform, floor, edge, crate, plate, goal, danger, beam, glass, climb, violet);

        GameObject hud = new GameObject("Game HUD");
        hud.AddComponent<GameHud>();

        EditorSceneManager.SaveScene(scene, ClimbGauntletScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ClimbGauntletScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Driftling Climb Gauntlet scene built at " + ClimbGauntletScenePath);
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
        Rigidbody plankBody = plank.GetComponent<Rigidbody>();
        plankBody.constraints = RigidbodyConstraints.None;

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

        Cube("Post Gate Ramp", new Vector3(0f, 0.6f, 24.2f), new Vector3(5.5f, 0.28f, 5f), floor, parent, true, 0f).transform.rotation = Quaternion.Euler(9f, 0f, 0f);

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
        BoxCollider goalCollider = goalZone.GetComponent<BoxCollider>();
        goalCollider.isTrigger = true;
        goalZone.AddComponent<GoalZone>();

        AddSign("Move, jump, then grab the wall with LMB/RMB.", new Vector3(-3.3f, 1.1f, -3.2f), parent);
        AddSign("Drag the blue crate onto the plate.", new Vector3(2.6f, 1.1f, 12.2f), parent);
        AddSign("Grab high, look down, and push forward.", new Vector3(-3.7f, 1.4f, 27.2f), parent);
    }

    private static void BuildClimbGauntletLevel(Transform parent, Material floor, Material edge, Material crate, Material plateMaterial, Material goal, Material danger, Material beamMaterial, Material glass, Material climb, Material violet)
    {
        Cube("Start Yard", new Vector3(0f, 0f, 0f), new Vector3(16f, 1f, 14f), floor, parent, false, 0f);
        Cube("First Summit Deck", new Vector3(0f, 4.25f, 10.5f), new Vector3(12f, 0.55f, 7f), floor, parent, true, 0f);
        Cube("Gate Yard", new Vector3(0f, 4.25f, 27f), new Vector3(18f, 0.55f, 15f), floor, parent, false, 0f);
        Cube("Chimney Landing", new Vector3(0f, 9.35f, 44f), new Vector3(13f, 0.55f, 8f), floor, parent, true, 0f);
        Cube("Traverse Rest Deck", new Vector3(0f, 9.35f, 60f), new Vector3(10f, 0.55f, 8f), floor, parent, true, 0f);
        Cube("Final Perch", new Vector3(0f, 16.4f, 82f), new Vector3(12f, 0.55f, 11f), floor, parent, true, 0f);

        Cube("Start Left Rail", new Vector3(-8.2f, 0.85f, 0f), new Vector3(0.3f, 1.25f, 12.5f), edge, parent, true, 0f);
        Cube("Start Right Rail", new Vector3(8.2f, 0.85f, 0f), new Vector3(0.3f, 1.25f, 12.5f), edge, parent, true, 0f);

        Cube("Warmup Climb Wall", new Vector3(0f, 2.65f, 5.85f), new Vector3(11f, 5.3f, 0.55f), climb, parent, true, 0f);
        AddAlternatingLedges("Warmup", 5.42f, 0.95f, 5, 1.95f, 0.84f, 1.8f, beamMaterial, parent);
        Cube("Warmup Top Lip", new Vector3(0f, 4.7f, 6.2f), new Vector3(11f, 0.32f, 1.25f), beamMaterial, parent, true, 0f);
        Cube("Short Wall Return", new Vector3(-4.9f, 2.5f, 11.2f), new Vector3(0.55f, 3.9f, 3.7f), climb, parent, true, 0f);
        Cube("Short Wall Return Ledge", new Vector3(-4.45f, 3.65f, 11.2f), new Vector3(0.85f, 0.26f, 2.8f), beamMaterial, parent, true, 0f);

        GameObject seesaw = Cube("Tilting Bridge", new Vector3(0f, 5.08f, 17.4f), new Vector3(2.4f, 0.28f, 9.3f), beamMaterial, parent, true, 5.2f);
        HingeJoint seesawHinge = seesaw.AddComponent<HingeJoint>();
        seesawHinge.axis = Vector3.right;
        seesawHinge.useLimits = true;
        JointLimits seesawLimits = seesawHinge.limits;
        seesawLimits.min = -14f;
        seesawLimits.max = 14f;
        seesawHinge.limits = seesawLimits;
        seesaw.GetComponent<Rigidbody>().angularDamping = 0.55f;
        Cube("Tilting Bridge Axle", new Vector3(0f, 4.7f, 17.4f), new Vector3(3.2f, 0.4f, 0.45f), edge, parent, true, 0f);

        GameObject gate = Cube("Heavy Lift Gate", new Vector3(0f, 6.25f, 34.2f), new Vector3(6.2f, 4.1f, 0.55f), danger, parent, true, 0f);
        GameObject plate = Cube("Crate Pressure Plate", new Vector3(-5.2f, 4.63f, 27f), new Vector3(2.4f, 0.18f, 2.4f), plateMaterial, parent, false, 0f);
        ConfigurePressurePlate(plate, gate, gate.transform.position + Vector3.up * 4.8f, 1.8f);
        GameObject crateA = Cube("Tall Blue Crate", new Vector3(-1.8f, 5.12f, 23.8f), new Vector3(1.3f, 1.3f, 1.3f), crate, parent, true, 2.1f);
        crateA.GetComponent<Rigidbody>().linearDamping = 0.22f;
        GameObject crateB = Cube("Spare Blue Crate", new Vector3(4.6f, 5.12f, 28.6f), new Vector3(1.15f, 1.15f, 1.15f), crate, parent, true, 1.7f);
        crateB.GetComponent<Rigidbody>().linearDamping = 0.24f;
        GameObject counterBall = Sphere("Rolling Gate Ball", new Vector3(4.8f, 5.05f, 23.6f), new Vector3(1.2f, 1.2f, 1.2f), violet, parent, true, 1.45f);
        counterBall.GetComponent<Rigidbody>().linearDamping = 0.12f;

        GameObject turnstile = Cube("Push Turnstile Beam", new Vector3(0f, 5.35f, 31f), new Vector3(11f, 0.35f, 0.35f), beamMaterial, parent, true, 3.4f);
        HingeJoint turnstileHinge = turnstile.AddComponent<HingeJoint>();
        turnstileHinge.axis = Vector3.up;
        turnstile.GetComponent<Rigidbody>().angularDamping = 0.35f;
        Cube("Turnstile Post", new Vector3(0f, 4.95f, 31f), new Vector3(0.45f, 1.25f, 0.45f), edge, parent, true, 0f);

        Cube("Chimney Left Wall", new Vector3(-3.15f, 6.95f, 40.2f), new Vector3(0.55f, 5.9f, 9.7f), climb, parent, true, 0f);
        Cube("Chimney Right Wall", new Vector3(3.15f, 6.95f, 40.2f), new Vector3(0.55f, 5.9f, 9.7f), climb, parent, true, 0f);
        Cube("Chimney Back Wall", new Vector3(0f, 6.65f, 44.8f), new Vector3(6.4f, 5.3f, 0.55f), climb, parent, true, 0f);
        AddChimneyHandholds(parent, beamMaterial);
        Cube("Chimney Top Lip", new Vector3(0f, 9.77f, 39.7f), new Vector3(6.5f, 0.3f, 1.1f), beamMaterial, parent, true, 0f);

        Cube("Side Traverse Wall", new Vector3(6.45f, 11.65f, 53.2f), new Vector3(0.6f, 4.9f, 17.5f), climb, parent, true, 0f);
        AddSideTraverseHolds(parent, beamMaterial);
        Cube("Glass Rest Bridge", new Vector3(0f, 9.88f, 52f), new Vector3(2.15f, 0.24f, 9.5f), glass, parent, true, 0f);
        Cube("Traverse Safety Rail", new Vector3(-5.4f, 10.3f, 59.8f), new Vector3(0.35f, 1.25f, 7f), edge, parent, true, 0f);

        GameObject swingBeam = Cube("Loose High Beam", new Vector3(0f, 10.15f, 66f), new Vector3(8.5f, 0.3f, 0.3f), beamMaterial, parent, true, 3.2f);
        HingeJoint swingHinge = swingBeam.AddComponent<HingeJoint>();
        swingHinge.axis = Vector3.forward;
        swingBeam.GetComponent<Rigidbody>().angularDamping = 0.28f;
        Cube("High Beam Stand", new Vector3(0f, 9.85f, 66f), new Vector3(0.5f, 1.15f, 0.5f), edge, parent, true, 0f);
        Cube("Narrow High Catwalk", new Vector3(0f, 9.95f, 70f), new Vector3(2.1f, 0.28f, 7.4f), floor, parent, true, 0f);

        Cube("Final Climb Wall", new Vector3(0f, 12.95f, 74.8f), new Vector3(12f, 7.2f, 0.65f), climb, parent, true, 0f);
        AddAlternatingLedges("Final", 74.35f, 10.1f, 8, 1.9f, 0.78f, 2.7f, beamMaterial, parent);
        Cube("Final Left Side Wall", new Vector3(-6.25f, 13.2f, 78.8f), new Vector3(0.55f, 6.4f, 6.8f), climb, parent, true, 0f);
        Cube("Final Right Side Wall", new Vector3(6.25f, 13.2f, 78.8f), new Vector3(0.55f, 6.4f, 6.8f), climb, parent, true, 0f);
        Cube("Final Top Lip", new Vector3(0f, 16.78f, 75.3f), new Vector3(12.4f, 0.32f, 1.3f), beamMaterial, parent, true, 0f);
        Cube("Goal Runway", new Vector3(0f, 16.4f, 91f), new Vector3(10f, 0.55f, 9f), floor, parent, true, 0f);

        GameObject goalZone = Cube("Gauntlet Goal Trigger", new Vector3(0f, 17.6f, 92.5f), new Vector3(3.6f, 2.4f, 3.6f), goal, parent, false, 0f);
        BoxCollider goalCollider = goalZone.GetComponent<BoxCollider>();
        goalCollider.isTrigger = true;
        goalZone.AddComponent<GoalZone>();

        AddSign("Hold LMB/RMB on walls, then hold Space to climb.", new Vector3(-3.7f, 1.25f, -4.5f), parent);
        AddSign("Use crates or the ball to hold the plate.", new Vector3(0f, 5.25f, 22.4f), parent);
        AddSign("The chimney rewards alternating grabs.", new Vector3(-4.4f, 6.3f, 36.7f), parent);
        AddSign("Traverse the side wall before the final tower.", new Vector3(-2.4f, 10.35f, 57.5f), parent);
    }

    private static void AddAlternatingLedges(string prefix, float z, float startY, int count, float width, float yStep, float xOffset, Material material, Transform parent)
    {
        for (int i = 0; i < count; i++)
        {
            float x = i % 2 == 0 ? -xOffset : xOffset;
            float y = startY + i * yStep;
            Cube(prefix + " Climb Ledge " + (i + 1), new Vector3(x, y, z), new Vector3(width, 0.25f, 0.85f), material, parent, true, 0f);
        }
    }

    private static void AddChimneyHandholds(Transform parent, Material material)
    {
        for (int i = 0; i < 6; i++)
        {
            float z = 36.8f + i * 1.35f;
            float y = 5.45f + i * 0.72f;
            Cube("Left Chimney Foot Rail " + (i + 1), new Vector3(-2.72f, y, z), new Vector3(0.8f, 0.22f, 0.8f), material, parent, true, 0f);
            Cube("Right Chimney Foot Rail " + (i + 1), new Vector3(2.72f, y + 0.34f, z + 0.55f), new Vector3(0.8f, 0.22f, 0.8f), material, parent, true, 0f);
        }
    }

    private static void AddSideTraverseHolds(Transform parent, Material material)
    {
        for (int i = 0; i < 8; i++)
        {
            float z = 46.2f + i * 2.05f;
            float y = 10.25f + (i % 3) * 0.48f;
            Cube("Side Traverse Hold " + (i + 1), new Vector3(6.02f, y, z), new Vector3(0.95f, 0.24f, 1.05f), material, parent, true, 0f);
        }
    }

    private static void ConfigurePressurePlate(GameObject plate, GameObject door, Vector3 openWorldPosition, float requiredMass)
    {
        BoxCollider plateCollider = plate.GetComponent<BoxCollider>();
        plateCollider.isTrigger = true;
        plateCollider.size = new Vector3(1f, 2.5f, 1f);
        plateCollider.center = new Vector3(0f, 0.98f, 0f);
        PressurePlate pressurePlate = plate.AddComponent<PressurePlate>();
        pressurePlate.controlledObject = door.transform;
        pressurePlate.closedWorldPosition = door.transform.position;
        pressurePlate.openWorldPosition = openWorldPosition;
        pressurePlate.requiredMass = requiredMass;
        pressurePlate.indicatorRenderer = plate.GetComponent<Renderer>();
    }

    private static WobblePlayerController CreatePlayer(Material bodyMaterial, Material handMaterial)
    {
        return CreatePlayer(bodyMaterial, handMaterial, new Vector3(0f, 2.2f, -4f));
    }

    private static WobblePlayerController CreatePlayer(Material bodyMaterial, Material handMaterial, Vector3 spawnPosition)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Driftling Player";
        player.transform.position = spawnPosition;
        player.transform.rotation = Quaternion.identity;
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
        Object.DestroyImmediate(head.GetComponent<Collider>());
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

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 4f, -10f);
        cameraObject.transform.rotation = Quaternion.Euler(12f, 0f, 0f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 63f;
        camera.nearClipPlane = 0.08f;
        camera.farClipPlane = 500f;
        camera.clearFlags = CameraClearFlags.Skybox;
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    private static void CreateLighting()
    {
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

        if (grabbable)
        {
            gameObject.AddComponent<Grabbable>();
        }

        if (mass > 0f)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

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

        if (grabbable)
        {
            gameObject.AddComponent<Grabbable>();
        }

        if (mass > 0f)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        return gameObject;
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

    private static Material MaterialAsset(string name, Color color, float smoothness)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
