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

    [Header("Body")]
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
    public float handReach = 2.55f;
    public float handPullForce = 36f;

    [Header("Hang Swing")]
    public float hangSwingStartForceScale = 0.15f;
    public float hangSwingInputBuildRate = 0.8f;
    public float hangSwingOpposeBrake = 12f;
    public float pendulumStarterForce = 2.5f;
    public float pendulumPumpForce = 16f;
    public float pendulumPumpAlignmentThreshold = 0.25f;
    public float pendulumMaxPumpAngleScale = 0.85f;
    public float pendulumGravityScale = 1.15f;
    public float pendulumMinInputAngle = 5f;
    public float pendulumFullInputAngle = 22f;
    public float pendulumRopeLength = 1.82f;
    public float pendulumRopeSlack = 0.12f;
    public float pendulumRopeCorrection = 32f;
    public float pendulumRopeDamping = 7f;
    public float pendulumRadialVelocityDamping = 0.85f;
    public float hangSwingEnergyGain = 1.45f;
    public float hangSwingPhysicalEnergyGain = 0.32f;
    public float hangSwingMinPhysicalSpeed = 0.35f;
    public float hangSwingEnergyDecay = 0.65f;
    public float hangSwingMaxEnergy = 1.8f;
    public float hangMaxUpwardSpeed = 3.2f;

    [Header("Grip Auto Release")]
    public float maxGripDuration = 4f;
    public float autoReleaseTension = 16f;
    public float autoReleaseTangentSpeed = 8.5f;

    [Header("Swing Launch")]
    public bool preserveReleaseVelocity = true;
    public float releaseImpulse = 2.5f;
    public float maxReleaseImpulse = 6f;
    public float minLaunchSpeedForImpulse = 2f;

    [Header("Grounding")]
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
    private bool leftGrabHeld;
    private bool rightGrabHeld;
    private bool leftHandPullHeld;
    private bool rightHandPullHeld;
    private float hangSwingEnergy;
    private float leftGripStartTime = -1f;
    private float rightGripStartTime = -1f;
    private bool leftWasActiveGrip;
    private bool rightWasActiveGrip;
    private bool leftAutoReleaseBlocked;
    private bool rightAutoReleaseBlocked;
    private Vector3 lastSwingDirection;
    private Vector3 lastGripPoint;
    private bool hasLastSwingDirection;
    private bool hasLastGripPoint;

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
        bool rawLeftGrabHeld = IsLeftGrabHeld();
        bool rawRightGrabHeld = IsRightGrabHeld();
        leftHandPullHeld = IsLeftHandPullHeld();
        rightHandPullHeld = IsRightHandPullHeld();
        if (!rawLeftGrabHeld)
        {
            leftAutoReleaseBlocked = false;
        }

        if (!rawRightGrabHeld)
        {
            rightAutoReleaseBlocked = false;
        }

        leftGrabHeld = rawLeftGrabHeld && !leftAutoReleaseBlocked;
        rightGrabHeld = rawRightGrabHeld && !rightAutoReleaseBlocked;

        HandleManualGripReleases();

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
            leftHand.SetRetractionHeld(leftHandPullHeld);
            leftHand.SetGrabHeld(leftGrabHeld);
        }

        if (rightHand != null)
        {
            rightHand.SetRetractionHeld(rightHandPullHeld);
            rightHand.SetGrabHeld(rightGrabHeld);
        }
    }

    private void FixedUpdate()
    {
        bool grounded = IsGrounded();
        bool hasGrip = HasAnyGrip();
        bool hasStaticGrip = HasAnyStaticGrip();
        if (ShouldSuppressWallGrounding(grounded, hasGrip))
        {
            grounded = false;
        }

        TrackGripState(IsActiveGrip(leftHand, leftGrabHeld), IsActiveGrip(rightHand, rightGrabHeld));
        ApplyMovement(grounded, hasGrip);
        ApplyHangSwing(grounded, hasGrip, hasStaticGrip);
        ApplyHandPull();
        ApplyGripAutoRelease();
        ApplyUprightTorque();
        UpdateHandTargets();

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
        leftGripStartTime = -1f;
        rightGripStartTime = -1f;
        leftWasActiveGrip = false;
        rightWasActiveGrip = false;
        leftHandPullHeld = false;
        rightHandPullHeld = false;
        leftAutoReleaseBlocked = false;
        rightAutoReleaseBlocked = false;
        hasLastSwingDirection = false;
        hasLastGripPoint = false;
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
        if (!grounded && !hasGrip)
        {
            desired = RemoveAirborneWallPress(desired);
        }

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

    private void ApplyHangSwing(bool grounded, bool hasGrip, bool hasStaticGrip)
    {
        if (grounded || !hasGrip || !TryGetGripPoint(out Vector3 gripPoint))
        {
            hangSwingEnergy = 0f;
            return;
        }

        Vector3 radial = body.position - gripPoint;
        if (radial.sqrMagnitude < 0.01f)
        {
            hangSwingEnergy = Mathf.MoveTowards(hangSwingEnergy, 0f, hangSwingEnergyDecay * Time.fixedDeltaTime);
            return;
        }

        radial.Normalize();
        lastGripPoint = gripPoint;
        hasLastGripPoint = true;

        ApplyPendulumRopeConstraint(gripPoint, radial);

        Vector3 pendulumGravity = Vector3.ProjectOnPlane(Physics.gravity, radial);
        if (pendulumGravity.sqrMagnitude > 0.001f)
        {
            body.AddForce(pendulumGravity * pendulumGravityScale, ForceMode.Acceleration);
        }

        Vector3 tangentVelocity = Vector3.ProjectOnPlane(body.linearVelocity, radial);
        float tangentSpeed = tangentVelocity.magnitude;
        if (tangentVelocity.sqrMagnitude > 0.04f)
        {
            lastSwingDirection = tangentVelocity.normalized;
            hasLastSwingDirection = true;
        }

        Vector3 swingInput = CameraScreenSwingDirection(radial);
        float inputStrength = Mathf.Clamp01(moveInput.magnitude);
        float angleFromDown = Vector3.Angle(radial, Vector3.down);
        float angleScale = Mathf.InverseLerp(pendulumMinInputAngle, pendulumFullInputAngle, angleFromDown);
        float energyDelta = 0f;

        if (inputStrength >= 0.05f && swingInput.sqrMagnitude > 0.001f)
        {
            Vector3 swingDirection = swingInput.normalized;
            if (!hasStaticGrip)
            {
                swingDirection = Vector3.ProjectOnPlane(swingDirection, Vector3.up);
                if (swingDirection.sqrMagnitude < 0.001f)
                {
                    hangSwingEnergy = Mathf.MoveTowards(hangSwingEnergy, 0f, hangSwingEnergyDecay * Time.fixedDeltaTime);
                    return;
                }

                swingDirection.Normalize();
            }

            if (body.linearVelocity.y >= hangMaxUpwardSpeed && swingDirection.y > 0f)
            {
                swingDirection = Vector3.ProjectOnPlane(swingDirection, Vector3.up);
                if (swingDirection.sqrMagnitude < 0.001f)
                {
                    hangSwingEnergy = Mathf.MoveTowards(hangSwingEnergy, 0f, hangSwingEnergyDecay * Time.fixedDeltaTime);
                    return;
                }

                swingDirection.Normalize();
            }

            bool hasMeaningfulSwing = tangentSpeed > hangSwingMinPhysicalSpeed;
            float alignment = hasMeaningfulSwing ? Vector3.Dot(tangentVelocity.normalized, swingDirection) : 0f;
            if (hasMeaningfulSwing && alignment < -0.05f)
            {
                float brake = hangSwingOpposeBrake * inputStrength * -alignment;
                body.AddForce(-tangentVelocity.normalized * brake, ForceMode.Acceleration);
                energyDelta -= brake * 0.18f;
            }
            else if (hasMeaningfulSwing && alignment >= pendulumPumpAlignmentThreshold)
            {
                float timedPump = alignment * tangentSpeed;
                float pumpAngleScale = Mathf.Lerp(0.35f, pendulumMaxPumpAngleScale, angleScale);
                float energyRatio = hangSwingMaxEnergy > 0f ? Mathf.Clamp01(hangSwingEnergy / hangSwingMaxEnergy) : 1f;
                float pumpScale = Mathf.Lerp(hangSwingStartForceScale, 1f, energyRatio) * pumpAngleScale;

                energyDelta += hangSwingInputBuildRate * inputStrength * pumpAngleScale;
                energyDelta += timedPump * hangSwingEnergyGain;
                if (alignment > 0f)
                {
                    energyDelta += (tangentSpeed - hangSwingMinPhysicalSpeed) * hangSwingPhysicalEnergyGain * alignment;
                }

                float pumpForce = pendulumPumpForce * inputStrength * pumpScale * alignment;
                body.AddForce(tangentVelocity.normalized * pumpForce, ForceMode.Acceleration);
            }
            else if (!hasMeaningfulSwing)
            {
                float starterScale = Mathf.Lerp(0.2f, 0.55f, angleScale);
                float starterForce = pendulumStarterForce * inputStrength * starterScale;
                body.AddForce(swingDirection * starterForce, ForceMode.Acceleration);
                energyDelta += hangSwingInputBuildRate * inputStrength * starterScale * 0.35f;
            }
            else
            {
                hangSwingEnergy = Mathf.MoveTowards(hangSwingEnergy, 0f, hangSwingEnergyDecay * Time.fixedDeltaTime);
                lastSwingDirection = tangentVelocity.normalized;
                hasLastSwingDirection = true;
                return;
            }

            if (hasMeaningfulSwing)
            {
                lastSwingDirection = tangentVelocity.normalized;
                hasLastSwingDirection = true;
            }
            else
            {
                lastSwingDirection = swingDirection;
                hasLastSwingDirection = true;
            }
        }

        if (energyDelta > 0f)
        {
            hangSwingEnergy = Mathf.Clamp(
                hangSwingEnergy + energyDelta * Time.fixedDeltaTime,
                0f,
                hangSwingMaxEnergy);
        }
        else if (energyDelta < 0f)
        {
            hangSwingEnergy = Mathf.MoveTowards(hangSwingEnergy, 0f, -energyDelta * Time.fixedDeltaTime);
        }
        else
        {
            hangSwingEnergy = Mathf.MoveTowards(hangSwingEnergy, 0f, hangSwingEnergyDecay * Time.fixedDeltaTime);
        }
    }

    private void ApplyPendulumRopeConstraint(Vector3 gripPoint, Vector3 radial)
    {
        float distance = Vector3.Distance(body.position, gripPoint);
        if (distance < 0.01f)
        {
            return;
        }

        float error = distance - pendulumRopeLength;
        if (Mathf.Abs(error) > pendulumRopeSlack)
        {
            float correctedError = error - Mathf.Sign(error) * pendulumRopeSlack;
            body.AddForce(-radial * (correctedError * pendulumRopeCorrection), ForceMode.Acceleration);
        }

        float radialSpeed = Vector3.Dot(body.linearVelocity, radial);
        if (Mathf.Abs(radialSpeed) > 0.01f)
        {
            float damping = radialSpeed * pendulumRopeDamping * pendulumRadialVelocityDamping;
            body.AddForce(-radial * damping, ForceMode.Acceleration);
        }
    }

    private void ApplyHandPull()
    {
        if (leftHandPullHeld)
        {
            PullBodyTowardHand(leftHand);
        }

        if (rightHandPullHeld)
        {
            PullBodyTowardHand(rightHand);
        }
    }

    private void PullBodyTowardHand(WobbleHand hand)
    {
        if (hand == null)
        {
            return;
        }

        Vector3 toHand = hand.transform.position - body.position;
        if (toHand.sqrMagnitude < 0.001f)
        {
            return;
        }

        Vector3 pullDirection = toHand.normalized;
        if (TryGetWallPressNormal(pullDirection, out Vector3 wallNormal))
        {
            pullDirection = Vector3.ProjectOnPlane(pullDirection, wallNormal);
            if (pullDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            pullDirection.Normalize();
        }

        body.AddForce(pullDirection * handPullForce, ForceMode.Acceleration);
    }

    private void HandleManualGripReleases()
    {
        bool releaseLeft = leftWasActiveGrip && !leftGrabHeld && leftHand != null && leftHand.IsGrabbed;
        bool releaseRight = rightWasActiveGrip && !rightGrabHeld && rightHand != null && rightHand.IsGrabbed;
        if (!releaseLeft && !releaseRight)
        {
            return;
        }

        bool leftRemains = leftWasActiveGrip && !releaseLeft;
        bool rightRemains = rightWasActiveGrip && !releaseRight;
        if (!leftRemains && !rightRemains)
        {
            ApplyReleaseImpulse();
        }

        if (releaseLeft)
        {
            leftHand.SetRetractionHeld(false);
            leftHand.ForceReleaseGrip();
            leftGripStartTime = -1f;
            leftWasActiveGrip = false;
        }

        if (releaseRight)
        {
            rightHand.SetRetractionHeld(false);
            rightHand.ForceReleaseGrip();
            rightGripStartTime = -1f;
            rightWasActiveGrip = false;
        }
    }

    private void TrackGripState(bool leftActive, bool rightActive)
    {
        bool hadAnyGrip = leftWasActiveGrip || rightWasActiveGrip;
        if (!hadAnyGrip && (leftActive || rightActive))
        {
            hangSwingEnergy = 0f;
            hasLastSwingDirection = false;
            hasLastGripPoint = false;
        }

        if (leftActive && !leftWasActiveGrip)
        {
            leftGripStartTime = Time.time;
        }
        else if (!leftActive)
        {
            leftGripStartTime = -1f;
        }

        if (rightActive && !rightWasActiveGrip)
        {
            rightGripStartTime = Time.time;
        }
        else if (!rightActive)
        {
            rightGripStartTime = -1f;
        }

        leftWasActiveGrip = leftActive;
        rightWasActiveGrip = rightActive;
    }

    private void ApplyGripAutoRelease()
    {
        bool leftActive = IsActiveGrip(leftHand, leftGrabHeld);
        bool rightActive = IsActiveGrip(rightHand, rightGrabHeld);
        if (!leftActive && !rightActive)
        {
            return;
        }

        bool releaseLeft = leftActive && ShouldAutoReleaseHand(leftHand, leftGripStartTime);
        bool releaseRight = rightActive && ShouldAutoReleaseHand(rightHand, rightGripStartTime);

        if (leftActive && rightActive && TryGetGripPoint(out Vector3 gripPoint))
        {
            float sharedTangentSpeed = TangentSpeedFromGripPoint(gripPoint);
            if (sharedTangentSpeed >= autoReleaseTangentSpeed)
            {
                releaseLeft = true;
                releaseRight = true;
            }
        }

        if (!releaseLeft && !releaseRight)
        {
            return;
        }

        bool releasesAllGrips = (!leftActive || releaseLeft) && (!rightActive || releaseRight);
        if (releasesAllGrips)
        {
            ApplyReleaseImpulse();
        }

        if (releaseLeft)
        {
            leftHand.SetRetractionHeld(false);
            leftHand.ForceReleaseGrip();
            leftHand.SetGrabHeld(false);
            leftGripStartTime = -1f;
            leftWasActiveGrip = false;
            leftAutoReleaseBlocked = true;
        }

        if (releaseRight)
        {
            rightHand.SetRetractionHeld(false);
            rightHand.ForceReleaseGrip();
            rightHand.SetGrabHeld(false);
            rightGripStartTime = -1f;
            rightWasActiveGrip = false;
            rightAutoReleaseBlocked = true;
        }

        if (releasesAllGrips)
        {
            hangSwingEnergy = 0f;
        }
    }

    private bool ShouldAutoReleaseHand(WobbleHand hand, float gripStartTime)
    {
        if (hand == null || !hand.IsGrabbed)
        {
            return false;
        }

        if (maxGripDuration > 0f && gripStartTime >= 0f && Time.time - gripStartTime >= maxGripDuration)
        {
            return true;
        }

        if (TangentSpeedFromGripPoint(hand.transform.position) >= autoReleaseTangentSpeed)
        {
            return true;
        }

        return EstimateGripTension(hand) >= autoReleaseTension;
    }

    private float TangentSpeedFromGripPoint(Vector3 gripPoint)
    {
        Vector3 radial = body.position - gripPoint;
        if (radial.sqrMagnitude < 0.01f)
        {
            return 0f;
        }

        return Vector3.ProjectOnPlane(body.linearVelocity, radial.normalized).magnitude;
    }

    private float EstimateGripTension(WobbleHand hand)
    {
        Vector3 radial = body.position - hand.transform.position;
        if (radial.sqrMagnitude < 0.01f)
        {
            return 0f;
        }

        Vector3 radialDirection = radial.normalized;
        float outwardSpeed = Mathf.Max(0f, Vector3.Dot(body.linearVelocity, radialDirection));
        float stretch = Mathf.Max(0f, radial.magnitude - handReach * 0.65f);
        return stretch * 4f + outwardSpeed * 2f + hangSwingEnergy * 2f;
    }

    private void ApplyReleaseImpulse()
    {
        if (releaseImpulse <= 0f || maxReleaseImpulse <= 0f)
        {
            return;
        }

        if (!TryGetReleaseDirection(out Vector3 direction))
        {
            return;
        }

        if (!preserveReleaseVelocity)
        {
            body.linearVelocity = Vector3.zero;
        }

        float currentSpeed = Mathf.Max(0f, Vector3.Dot(body.linearVelocity, direction));
        float lowSpeedBoost = Mathf.Max(0f, minLaunchSpeedForImpulse - currentSpeed);
        float impulseMagnitude = Mathf.Clamp(releaseImpulse + lowSpeedBoost + hangSwingEnergy, 0f, maxReleaseImpulse);
        body.AddForce(direction * impulseMagnitude, ForceMode.VelocityChange);
    }

    private bool TryGetReleaseDirection(out Vector3 direction)
    {
        direction = Vector3.zero;

        if (hasLastGripPoint)
        {
            Vector3 radial = body.position - lastGripPoint;
            if (radial.sqrMagnitude > 0.01f)
            {
                Vector3 tangentVelocity = Vector3.ProjectOnPlane(body.linearVelocity, radial.normalized);
                if (tangentVelocity.sqrMagnitude > 0.04f)
                {
                    direction = tangentVelocity.normalized;
                    return true;
                }
            }
        }

        if (hasLastSwingDirection && lastSwingDirection.sqrMagnitude > 0.001f)
        {
            direction = lastSwingDirection.normalized;
            return true;
        }

        Vector3 fallback = CameraForwardPlanar();
        if (fallback.sqrMagnitude > 0.001f)
        {
            direction = fallback.normalized;
            return true;
        }

        return false;
    }

    private Vector3 CameraScreenSwingDirection(Vector3 radial)
    {
        Vector3 screenRight = cameraTransform != null ? cameraTransform.right : transform.right;
        Vector3 screenUp = cameraTransform != null ? cameraTransform.up : Vector3.up;
        Vector3 desired = screenRight * moveInput.x + screenUp * moveInput.y;
        desired = Vector3.ProjectOnPlane(desired, radial);
        return desired.sqrMagnitude > 0.001f ? desired.normalized : Vector3.zero;
    }

    private bool IsPressingIntoWall(Vector3 moveDirection)
    {
        return TryGetWallPressNormal(moveDirection, out _);
    }

    private Vector3 RemoveAirborneWallPress(Vector3 desired)
    {
        if (desired.sqrMagnitude < 0.001f || !TryGetWallPressNormal(desired.normalized, out Vector3 wallNormal))
        {
            return desired;
        }

        Vector3 adjusted = Vector3.ProjectOnPlane(desired, wallNormal);
        adjusted = Vector3.ProjectOnPlane(adjusted, Vector3.up);
        return adjusted.sqrMagnitude > 0.001f ? adjusted : Vector3.zero;
    }

    private bool TryGetWallPressNormal(Vector3 moveDirection, out Vector3 wallNormal)
    {
        return TryGetWallPressNormalFrom(body.position + Vector3.up * 0.45f, moveDirection, out wallNormal)
            || TryGetWallPressNormalFrom(body.position, moveDirection, out wallNormal)
            || TryGetWallPressNormalFrom(body.position + Vector3.down * 0.45f, moveDirection, out wallNormal);
    }

    private bool TryGetWallPressNormalFrom(Vector3 origin, Vector3 moveDirection, out Vector3 wallNormal)
    {
        wallNormal = Vector3.zero;
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

            if (Vector3.Dot(moveDirection, hit.normal) >= -0.05f)
            {
                continue;
            }

            wallNormal = hit.normal;
            return true;
        }

        return false;
    }

    private bool ShouldSuppressWallGrounding(bool grounded, bool hasGrip)
    {
        if (!grounded || hasGrip)
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

    private bool HasAnyStaticGrip()
    {
        return IsActiveStaticGrip(leftHand, leftGrabHeld) || IsActiveStaticGrip(rightHand, rightGrabHeld);
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

    private static bool IsActiveStaticGrip(WobbleHand hand, bool grabInputHeld)
    {
        return IsActiveGrip(hand, grabInputHeld) && hand.HasStaticGrip;
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

    private bool IsLeftGrabHeld()
    {
#if ENABLE_INPUT_SYSTEM
        bool mouse = Mouse.current != null && Mouse.current.leftButton.isPressed;
        if (mouse)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(0);
#else
        return false;
#endif
    }

    private bool IsRightGrabHeld()
    {
#if ENABLE_INPUT_SYSTEM
        bool mouse = Mouse.current != null && Mouse.current.rightButton.isPressed;
        if (mouse)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(1);
#else
        return false;
#endif
    }

    private bool IsLeftHandPullHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.qKey.isPressed)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.Q);
#else
        return false;
#endif
    }

    private bool IsRightHandPullHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.isPressed)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.E);
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
