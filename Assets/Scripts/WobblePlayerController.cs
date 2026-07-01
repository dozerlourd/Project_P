using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody))]
public sealed class WobblePlayerController : MonoBehaviour
{
    public Rigidbody body;
    public WobbleHand leftHand;
    public WobbleHand rightHand;
    public Transform cameraTransform;
    public Transform head;

    public Vector3 leftShoulderLocal = new Vector3(-0.42f, 0.58f, 0.08f);
    public Vector3 rightShoulderLocal = new Vector3(0.42f, 0.58f, 0.08f);
    public float moveForce = 38f;
    public float airControl = 0.45f;
    public float maxPlanarSpeed = 5.8f;
    public float jumpVelocity = 5.4f;
    public float uprightSpring = 42f;
    public float uprightDamping = 7f;
    public float turnSpeed = 7f;
    public float cameraDistance = 6.2f;
    public float cameraHeight = 1.05f;
    public float mouseSensitivity = 0.12f;
    public float handReach = 2.35f;
    public float climbAssist = 40f;
    public float hangClimbRiseSpeed = 1.55f;
    public float hangSwingPumpForce = 18f;
    public float hangSwingLiftForce = 15f;
    public float hangSwingEnergyGain = 1.45f;
    public float hangSwingPhysicalEnergyGain = 0.32f;
    public float hangSwingMinPhysicalSpeed = 0.35f;
    public float hangSwingEnergyDecay = 0.65f;
    public float hangSwingMaxEnergy = 1.8f;
    public float groundProbeRadius = 0.28f;
    public float groundProbeDistance = 1.15f;
    public float groundNormalMinY = 0.62f;
    public float groundMaxCenterOffset = 0.48f;
    public float groundMinCenterHeight = 0.55f;
    public float centralGroundProbeRadius = 0.12f;
    public float wallSlideProbeRadius = 0.42f;
    public float wallSlideProbeDistance = 0.42f;
    public string mapLayerName = "WobbleMap";

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Vector2 moveInput;
    private float yaw;
    private float pitch;
    private bool jumpQueued;
    private bool jumpHeld;
    private bool leftGrabHeld;
    private bool rightGrabHeld;
    private float hangSwingEnergy;

    private void Awake()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        yaw = transform.eulerAngles.y;
        pitch = 12f;

        if (leftHand != null)
        {
            leftHand.Configure(body, leftShoulderLocal, handReach);
        }

        if (rightHand != null)
        {
            rightHand.Configure(body, rightShoulderLocal, handReach);
        }

        IgnoreSelfCollisions(leftHand);
        IgnoreSelfCollisions(rightHand);
        ApplyMapLayerToSceneObjects();
        DisableHandMapCollisionsForSpawn();
        LockCursor(true);
    }

    private void Update()
    {
        ReadLookInput();
        moveInput = ReadMoveInput();
        jumpQueued = jumpQueued || WasJumpPressed();
        jumpHeld = IsJumpHeld();
        leftGrabHeld = IsLeftGrabHeld();
        rightGrabHeld = IsRightGrabHeld();

        if (WasRespawnPressed())
        {
            Respawn();
        }

        if (WasCancelPressed())
        {
            LockCursor(false);
        }

        if (WasPrimaryLookPressed())
        {
            LockCursor(true);
        }

        if (leftHand != null)
        {
            leftHand.SetGrabHeld(leftGrabHeld);
        }

        if (rightHand != null)
        {
            rightHand.SetGrabHeld(rightGrabHeld);
        }
    }

    private void FixedUpdate()
    {
        bool grounded = IsGrounded();
        bool hasGrip = HasAnyGrip();
        if (ShouldSuppressWallGrounding(grounded, hasGrip))
        {
            grounded = false;
        }

        ApplyMovement(grounded, hasGrip);
        ApplyHangSwing(grounded, hasGrip);
        ApplyHangClimb(grounded, hasGrip, jumpHeld);
        ApplyUprightTorque();
        UpdateHandTargets();

        if (hasGrip && moveInput.y > 0.05f)
        {
            // Holding forward while grabbed helps pull the body forward; climbing upward is a Space action.
            body.AddForce(CameraForwardPlanar() * (climbAssist * 0.23f), ForceMode.Acceleration);
        }

        jumpQueued = false;
    }

    private void LateUpdate()
    {
        UpdateCamera();
        UpdateHead();
    }

    public void Respawn()
    {
        body.position = spawnPosition;
        body.rotation = spawnRotation;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        if (leftHand != null)
        {
            leftHand.ResetHand(body.transform.TransformPoint(leftShoulderLocal + Vector3.forward * 0.45f));
        }

        if (rightHand != null)
        {
            rightHand.ResetHand(body.transform.TransformPoint(rightShoulderLocal + Vector3.forward * 0.45f));
        }

        hangSwingEnergy = 0f;
        GameHud.ShowMessage("Respawned.", 1.6f);
    }

    private void DisableHandMapCollisionsForSpawn()
    {
        if (leftHand != null)
        {
            leftHand.DisableMapCollisionForSeconds(2f);
        }

        if (rightHand != null)
        {
            rightHand.DisableMapCollisionForSeconds(2f);
        }
    }

    private void ApplyMapLayerToSceneObjects()
    {
        int mapLayer = LayerMask.NameToLayer(mapLayerName);
        if (mapLayer < 0)
        {
            return;
        }

        Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider candidate = colliders[i];
            if (ShouldSkipMapLayer(candidate))
            {
                continue;
            }

            candidate.gameObject.layer = mapLayer;
        }
    }

    private bool ShouldSkipMapLayer(Collider candidate)
    {
        if (candidate == null || candidate.isTrigger)
        {
            return true;
        }

        if (candidate.attachedRigidbody == body)
        {
            return true;
        }

        return candidate.GetComponentInParent<WobblePlayerController>() != null
            || candidate.GetComponentInParent<WobbleHand>() != null;
    }

    private void ApplyMovement(bool grounded, bool hasGrip)
    {
        Vector3 desired = CameraForwardPlanar() * moveInput.y + CameraRightPlanar() * moveInput.x;

        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
        if (desired.sqrMagnitude > 0.001f && horizontalVelocity.magnitude < maxPlanarSpeed)
        {
            float control = grounded ? 1f : airControl;
            body.AddForce(desired * (moveForce * control), ForceMode.Acceleration);
        }

        if (desired.sqrMagnitude > 0.02f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desired.normalized, Vector3.up);
            body.MoveRotation(Quaternion.Slerp(body.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        }

        if (jumpQueued && grounded)
        {
            Vector3 velocity = body.linearVelocity;
            if (velocity.y < 0f)
            {
                velocity.y = 0f;
                body.linearVelocity = velocity;
            }

            body.AddForce(Vector3.up * jumpVelocity, ForceMode.VelocityChange);
        }
    }

    private void ApplyHangClimb(bool grounded, bool hasGrip, bool climbHeld)
    {
        if (grounded || !hasGrip || !TryGetGripPoint(out Vector3 gripPoint))
        {
            return;
        }

        if (!climbHeld)
        {
            return;
        }

        Vector3 velocity = body.linearVelocity;
        float climbAcceleration = Mathf.Clamp((hangClimbRiseSpeed - velocity.y) * 8f - Physics.gravity.y, 0f, climbAssist);
        body.AddForce(Vector3.up * climbAcceleration, ForceMode.Acceleration);

        Vector3 toGripPlanar = Vector3.ProjectOnPlane(gripPoint - body.position, Vector3.up);
        if (toGripPlanar.sqrMagnitude > 0.01f)
        {
            body.AddForce(toGripPlanar.normalized * (climbAssist * 0.18f), ForceMode.Acceleration);
        }
    }

    private void ApplyHangSwing(bool grounded, bool hasGrip)
    {
        if (grounded || !hasGrip || !TryGetGripPoint(out Vector3 gripPoint))
        {
            hangSwingEnergy = 0f;
            return;
        }

        Vector3 toBody = body.position - gripPoint;
        Vector3 swingAxis = Vector3.Cross(Vector3.up, toBody);
        if (swingAxis.sqrMagnitude < 0.01f)
        {
            swingAxis = CameraRightPlanar();
        }
        else
        {
            swingAxis.Normalize();
        }

        if (Vector3.Dot(swingAxis, CameraRightPlanar()) < 0f)
        {
            swingAxis = -swingAxis;
        }

        Vector3 lateralVelocity = Vector3.Project(body.linearVelocity, swingAxis);
        float lateralSpeed = Mathf.Abs(Vector3.Dot(lateralVelocity, swingAxis));
        float sideInput = moveInput.x;
        float energyDelta = 0f;

        if (lateralSpeed > hangSwingMinPhysicalSpeed)
        {
            energyDelta += (lateralSpeed - hangSwingMinPhysicalSpeed) * hangSwingPhysicalEnergyGain;
        }

        if (Mathf.Abs(sideInput) >= 0.05f)
        {
            Vector3 swingDirection = swingAxis * Mathf.Sign(sideInput);
            float timedPump = Mathf.Max(0.15f, Vector3.Dot(lateralVelocity, swingDirection));
            energyDelta += timedPump * hangSwingEnergyGain;

            float pumpForce = hangSwingPumpForce * (1f + hangSwingEnergy);
            body.AddForce(swingDirection * pumpForce, ForceMode.Acceleration);
        }

        if (energyDelta > 0f)
        {
            hangSwingEnergy = Mathf.Clamp(
                hangSwingEnergy + energyDelta * Time.fixedDeltaTime,
                0f,
                hangSwingMaxEnergy);
        }
        else
        {
            hangSwingEnergy = Mathf.MoveTowards(hangSwingEnergy, 0f, hangSwingEnergyDecay * Time.fixedDeltaTime);
        }

        if (hangSwingEnergy <= 0f)
        {
            return;
        }

        float liftInputScale = Mathf.Max(Mathf.Abs(sideInput), Mathf.InverseLerp(hangSwingMinPhysicalSpeed, hangSwingMinPhysicalSpeed * 3f, lateralSpeed));
        float liftForce = hangSwingLiftForce * hangSwingEnergy * liftInputScale;
        body.AddForce(Vector3.up * liftForce, ForceMode.Acceleration);
    }

    private bool IsPressingIntoWall(Vector3 moveDirection)
    {
        return IsPressingIntoWallFrom(body.position + Vector3.up * 0.45f, moveDirection)
            || IsPressingIntoWallFrom(body.position, moveDirection)
            || IsPressingIntoWallFrom(body.position + Vector3.down * 0.45f, moveDirection);
    }

    private bool IsPressingIntoWallFrom(Vector3 origin, Vector3 moveDirection)
    {
        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            wallSlideProbeRadius,
            moveDirection,
            wallSlideProbeDistance,
            ~0,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.normal.y >= groundNormalMinY)
            {
                continue;
            }

            if (hit.rigidbody == body || hit.collider.GetComponentInParent<WobblePlayerController>() == this)
            {
                continue;
            }

            if (leftHand != null && hit.collider.GetComponentInParent<WobbleHand>() == leftHand)
            {
                continue;
            }

            if (rightHand != null && hit.collider.GetComponentInParent<WobbleHand>() == rightHand)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool ShouldSuppressWallGrounding(bool grounded, bool hasGrip)
    {
        if (!grounded || hasGrip || moveInput.y <= 0.05f)
        {
            return false;
        }

        Vector3 moveDirection = CameraForwardPlanar() * moveInput.y + CameraRightPlanar() * moveInput.x;
        moveDirection = Vector3.ProjectOnPlane(moveDirection, Vector3.up);
        if (moveDirection.sqrMagnitude < 0.01f || !IsPressingIntoWall(moveDirection.normalized))
        {
            return false;
        }

        return !HasCentralGroundSupport();
    }

    private bool HasCentralGroundSupport()
    {
        Vector3 origin = body.position + Vector3.up * 0.15f;
        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            centralGroundProbeRadius,
            Vector3.down,
            groundProbeDistance,
            ~0,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            if (IsValidGroundHit(hits[i]))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasAnyGrip()
    {
        return IsActiveGrip(leftHand, leftGrabHeld) || IsActiveGrip(rightHand, rightGrabHeld);
    }

    private bool TryGetGripPoint(out Vector3 gripPoint)
    {
        gripPoint = Vector3.zero;
        int count = 0;

        if (IsActiveGrip(leftHand, leftGrabHeld))
        {
            gripPoint += leftHand.transform.position;
            count++;
        }

        if (IsActiveGrip(rightHand, rightGrabHeld))
        {
            gripPoint += rightHand.transform.position;
            count++;
        }

        if (count == 0)
        {
            return false;
        }

        gripPoint /= count;
        return true;
    }

    private static bool IsActiveGrip(WobbleHand hand, bool grabInputHeld)
    {
        return grabInputHeld && hand != null && hand.GrabHeld && hand.IsGrabbed;
    }

    private void ApplyUprightTorque()
    {
        Vector3 axis = Vector3.Cross(transform.up, Vector3.up);
        Vector3 torque = axis * uprightSpring - body.angularVelocity * uprightDamping;
        body.AddTorque(torque, ForceMode.Acceleration);
    }

    private void UpdateHandTargets()
    {
        if (leftHand != null)
        {
            leftHand.SetTarget(HandTarget(leftShoulderLocal, -1f, leftGrabHeld));
        }

        if (rightHand != null)
        {
            rightHand.SetTarget(HandTarget(rightShoulderLocal, 1f, rightGrabHeld));
        }
    }

    private Vector3 HandTarget(Vector3 shoulderLocal, float side, bool extended)
    {
        Vector3 shoulder = body.transform.TransformPoint(shoulderLocal);
        if (extended)
        {
            return shoulder + CameraAim() * handReach + CameraRightPlanar() * (side * 0.18f);
        }

        return shoulder + transform.forward * 0.28f + transform.right * (side * 0.14f) + Vector3.down * 0.45f;
    }

    private void UpdateCamera()
    {
        if (cameraTransform == null)
        {
            return;
        }

        Quaternion lookRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = body.position + Vector3.up * cameraHeight;
        Vector3 desired = pivot - lookRotation * Vector3.forward * cameraDistance;
        Vector3 direction = desired - pivot;
        float distance = direction.magnitude;

        if (distance > 0.01f && Physics.SphereCast(pivot, 0.25f, direction.normalized, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.rigidbody != body)
            {
                desired = pivot + direction.normalized * Mathf.Max(0.5f, hit.distance - 0.2f);
            }
        }

        cameraTransform.SetPositionAndRotation(Vector3.Lerp(cameraTransform.position, desired, 18f * Time.deltaTime), lookRotation);
    }

    private void UpdateHead()
    {
        if (head == null || cameraTransform == null)
        {
            return;
        }

        head.rotation = Quaternion.Slerp(head.rotation, Quaternion.LookRotation(CameraAim(), Vector3.up), 12f * Time.deltaTime);
    }

    private bool IsGrounded()
    {
        Vector3 origin = body.position + Vector3.up * 0.15f;
        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            groundProbeRadius,
            Vector3.down,
            groundProbeDistance,
            ~0,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (!IsValidGroundHit(hit))
            {
                continue;
            }

            Vector3 centerToHit = Vector3.ProjectOnPlane(hit.point - body.position, Vector3.up);
            if (centerToHit.magnitude > groundMaxCenterOffset)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool IsValidGroundHit(RaycastHit hit)
    {
        if (hit.collider == null || hit.normal.y < groundNormalMinY)
        {
            return false;
        }

        if (body.position.y - hit.point.y < groundMinCenterHeight)
        {
            return false;
        }

        if (hit.rigidbody == body || hit.collider.GetComponentInParent<WobblePlayerController>() == this)
        {
            return false;
        }

        if (leftHand != null && hit.collider.GetComponentInParent<WobbleHand>() == leftHand)
        {
            return false;
        }

        if (rightHand != null && hit.collider.GetComponentInParent<WobbleHand>() == rightHand)
        {
            return false;
        }

        return true;
    }

    private Vector3 CameraAim()
    {
        if (cameraTransform != null)
        {
            return cameraTransform.forward.normalized;
        }

        return transform.forward;
    }

    private Vector3 CameraForwardPlanar()
    {
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
        forward = Vector3.ProjectOnPlane(forward, Vector3.up);
        return forward.sqrMagnitude > 0.001f ? forward.normalized : transform.forward;
    }

    private Vector3 CameraRightPlanar()
    {
        Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;
        right = Vector3.ProjectOnPlane(right, Vector3.up);
        return right.sqrMagnitude > 0.001f ? right.normalized : transform.right;
    }

    private void IgnoreSelfCollisions(WobbleHand hand)
    {
        if (hand == null)
        {
            return;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            hand.IgnoreCollision(colliders[i]);
        }
    }

    private void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void ReadLookInput()
    {
        Vector2 look = ReadLookDelta();
        yaw += look.x * mouseSensitivity;
        pitch -= look.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -58f, 72f);
    }

    private Vector2 ReadMoveInput()
    {
        Vector2 value = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) value.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) value.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) value.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) value.y += 1f;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        value += new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif

        return new Vector2(Mathf.Clamp(value.x, -1f, 1f), Mathf.Clamp(value.y, -1f, 1f));
    }

    private Vector2 ReadLookDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.delta.ReadValue();
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f;
#else
        return Vector2.zero;
#endif
    }

    private bool WasJumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space);
#else
        return false;
#endif
    }

    private bool IsJumpHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.Space);
#else
        return false;
#endif
    }

    private bool IsLeftGrabHeld()
    {
#if ENABLE_INPUT_SYSTEM
        bool mouse = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool key = Keyboard.current != null && Keyboard.current.qKey.isPressed;
        if (mouse || key)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(0) || Input.GetKey(KeyCode.Q);
#else
        return false;
#endif
    }

    private bool IsRightGrabHeld()
    {
#if ENABLE_INPUT_SYSTEM
        bool mouse = Mouse.current != null && Mouse.current.rightButton.isPressed;
        bool key = Keyboard.current != null && Keyboard.current.eKey.isPressed;
        if (mouse || key)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(1) || Input.GetKey(KeyCode.E);
#else
        return false;
#endif
    }

    private bool WasRespawnPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.R);
#else
        return false;
#endif
    }

    private bool WasCancelPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }

    private bool WasPrimaryLookPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }
}
