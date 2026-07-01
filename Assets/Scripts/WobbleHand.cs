using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public sealed class WobbleHand : MonoBehaviour
{
    public Rigidbody playerBody;
    public Vector3 shoulderLocal;
    public float reach = 2.35f;
    public float followSpring = 132f;
    public float followDamping = 13f;
    public float grabbedSpring = 92f;
    public float grabbedDamping = 10f;
    public float gripBreakForce = 1800f;
    public float surfaceGripPadding = 0.025f;
    public bool allowStaticWorldGrip = true;
    public string mapLayerName = "WobbleMap";
    public string noMapCollisionLayerName = "WobbleHandNoMap";

    private Rigidbody handBody;
    private Collider handCollider;
    private ConfigurableJoint armJoint;
    private FixedJoint gripJoint;
    private Vector3 targetPosition;
    private bool grabHeld;
    private bool mapCollisionInputEnabled;
    private float mapCollisionDisabledUntil;
    private int activeLayer;
    private int noMapCollisionLayer = -1;

    public bool IsGrabbed
    {
        get { return gripJoint != null; }
    }

    public bool GrabHeld
    {
        get { return grabHeld; }
    }

    private void Awake()
    {
        handBody = GetComponent<Rigidbody>();
        handCollider = GetComponent<Collider>();
        targetPosition = transform.position;
        activeLayer = gameObject.layer;
        noMapCollisionLayer = LayerMask.NameToLayer(noMapCollisionLayerName);
        ConfigureMapLayerCollision();
        UpdateMapCollisionState();
    }

    public void Configure(Rigidbody connectedBody, Vector3 connectedShoulderLocal, float maxReach)
    {
        playerBody = connectedBody;
        shoulderLocal = connectedShoulderLocal;
        reach = maxReach;

        if (armJoint == null)
        {
            armJoint = gameObject.AddComponent<ConfigurableJoint>();
        }

        armJoint.connectedBody = playerBody;
        armJoint.autoConfigureConnectedAnchor = false;
        armJoint.connectedAnchor = shoulderLocal;
        armJoint.anchor = Vector3.zero;
        armJoint.xMotion = ConfigurableJointMotion.Limited;
        armJoint.yMotion = ConfigurableJointMotion.Limited;
        armJoint.zMotion = ConfigurableJointMotion.Limited;
        armJoint.angularXMotion = ConfigurableJointMotion.Free;
        armJoint.angularYMotion = ConfigurableJointMotion.Free;
        armJoint.angularZMotion = ConfigurableJointMotion.Free;
        armJoint.enableCollision = false;
        armJoint.enablePreprocessing = false;
        armJoint.projectionMode = JointProjectionMode.PositionAndRotation;
        armJoint.projectionDistance = 0.15f;

        SoftJointLimit limit = armJoint.linearLimit;
        limit.limit = reach;
        armJoint.linearLimit = limit;

        SoftJointLimitSpring spring = armJoint.linearLimitSpring;
        spring.spring = 120f;
        spring.damper = 12f;
        armJoint.linearLimitSpring = spring;
    }

    public void IgnoreCollision(Collider other)
    {
        if (handCollider != null && other != null)
        {
            Physics.IgnoreCollision(handCollider, other, true);
        }
    }

    public void SetTarget(Vector3 worldPosition)
    {
        targetPosition = worldPosition;
    }

    public void SetGrabHeld(bool held)
    {
        grabHeld = held;
        mapCollisionInputEnabled = held;
        if (!grabHeld)
        {
            ReleaseGrip();
        }

        UpdateMapCollisionState();
    }

    public void ResetHand(Vector3 worldPosition)
    {
        grabHeld = false;
        mapCollisionInputEnabled = false;
        ReleaseGrip();

        Vector3 safePosition = ResolveSpawnOverlap(worldPosition);
        targetPosition = safePosition;
        transform.position = safePosition;
        handBody.position = safePosition;
        handBody.rotation = Quaternion.identity;
        handBody.linearVelocity = Vector3.zero;
        handBody.angularVelocity = Vector3.zero;
        DisableMapCollisionForSeconds(2f);
    }

    public void DisableMapCollisionForSeconds(float seconds)
    {
        mapCollisionDisabledUntil = Mathf.Max(mapCollisionDisabledUntil, Time.time + seconds);
        UpdateMapCollisionState();
    }

    private void FixedUpdate()
    {
        UpdateMapCollisionState();

        float spring = IsGrabbed ? grabbedSpring : followSpring;
        float damping = IsGrabbed ? grabbedDamping : followDamping;
        Vector3 toTarget = targetPosition - handBody.position;
        Vector3 acceleration = toTarget * spring - handBody.linearVelocity * damping;
        handBody.AddForce(acceleration, ForceMode.Acceleration);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryGrip(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryGrip(collision);
    }

    private void TryGrip(Collision collision)
    {
        if (!grabHeld || !IsMapCollisionEnabled() || gripJoint != null || collision.collider == null || collision.collider.isTrigger || collision.contactCount == 0)
        {
            return;
        }

        if (collision.rigidbody == playerBody || collision.collider.GetComponentInParent<WobblePlayerController>() != null)
        {
            return;
        }

        if (collision.collider.GetComponentInParent<WobbleHand>() != null)
        {
            return;
        }

        Grabbable grabbable = collision.collider.GetComponentInParent<Grabbable>();
        bool canGrip = grabbable != null && grabbable.allowGrip;
        canGrip = canGrip || (allowStaticWorldGrip && collision.rigidbody == null);

        if (!canGrip)
        {
            return;
        }

        ContactPoint contact = collision.GetContact(0);
        Vector3 gripPoint = contact.point;
        Vector3 gripPosition = gripPoint + contact.normal * GripSurfaceOffset();

        targetPosition = gripPosition;
        transform.position = gripPosition;
        handBody.position = gripPosition;
        handBody.linearVelocity = Vector3.zero;
        handBody.angularVelocity = Vector3.zero;

        gripJoint = gameObject.AddComponent<FixedJoint>();
        gripJoint.connectedBody = collision.rigidbody;
        gripJoint.autoConfigureConnectedAnchor = false;
        gripJoint.anchor = transform.InverseTransformPoint(gripPoint);
        gripJoint.connectedAnchor = collision.rigidbody != null
            ? collision.rigidbody.transform.InverseTransformPoint(gripPoint)
            : gripPoint;
        gripJoint.breakForce = gripBreakForce;
        gripJoint.breakTorque = gripBreakForce;
        gripJoint.enableCollision = false;
        gripJoint.connectedMassScale = 1f;
        gripJoint.massScale = 0.4f;
    }

    private Vector3 ResolveSpawnOverlap(Vector3 position)
    {
        if (handCollider == null)
        {
            return position;
        }

        float radius = GripSurfaceOffset();
        Vector3 resolved = position;

        for (int step = 0; step < 4; step++)
        {
            bool moved = false;
            Collider[] overlaps = Physics.OverlapSphere(resolved, radius, ~0, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider other = overlaps[i];
                if (ShouldIgnoreSpawnOverlap(other))
                {
                    continue;
                }

                if (Physics.ComputePenetration(
                    handCollider,
                    resolved,
                    handBody.rotation,
                    other,
                    other.transform.position,
                    other.transform.rotation,
                    out Vector3 direction,
                    out float distance))
                {
                    resolved += direction * (distance + surfaceGripPadding);
                    moved = true;
                }
            }

            if (!moved)
            {
                break;
            }
        }

        return resolved;
    }

    private bool ShouldIgnoreSpawnOverlap(Collider other)
    {
        if (other == null || other == handCollider || other.isTrigger)
        {
            return true;
        }

        if (other.attachedRigidbody == handBody || other.attachedRigidbody == playerBody)
        {
            return true;
        }

        return other.GetComponentInParent<WobbleHand>() != null || other.GetComponentInParent<WobblePlayerController>() != null;
    }

    private float GripSurfaceOffset()
    {
        if (handCollider == null)
        {
            return 0.18f;
        }

        Vector3 extents = handCollider.bounds.extents;
        return Mathf.Max(0.02f, Mathf.Min(extents.x, extents.y, extents.z) + surfaceGripPadding);
    }

    private void ReleaseGrip()
    {
        if (gripJoint == null)
        {
            return;
        }

        Destroy(gripJoint);
        gripJoint = null;
    }

    private bool IsMapCollisionEnabled()
    {
        return gameObject.layer == activeLayer;
    }

    private void UpdateMapCollisionState()
    {
        if (noMapCollisionLayer < 0)
        {
            return;
        }

        bool shouldEnable = mapCollisionInputEnabled && Time.time >= mapCollisionDisabledUntil;
        int targetLayer = shouldEnable ? activeLayer : noMapCollisionLayer;
        if (gameObject.layer == targetLayer)
        {
            return;
        }

        if (!shouldEnable)
        {
            ReleaseGrip();
        }

        gameObject.layer = targetLayer;
    }

    private void ConfigureMapLayerCollision()
    {
        int mapLayer = LayerMask.NameToLayer(mapLayerName);
        if (mapLayer < 0 || noMapCollisionLayer < 0)
        {
            return;
        }

        Physics.IgnoreLayerCollision(noMapCollisionLayer, mapLayer, true);
    }
}
