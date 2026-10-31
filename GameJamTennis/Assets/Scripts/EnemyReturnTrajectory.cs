using UnityEngine;

public class EnemyReturnTrajectory : MonoBehaviour
{
    private enum FlightLeg
    {
        ToTable,
        ToPlayer,
        Complete
    }

    private Rigidbody ballBody;
    private Collider tableArea;
    private Collider playerHitbox;
    private Collider ballCollider;
    private Collider net;
    private Vector3 tableLandingPoint;
    private Vector3 playerTarget;
    private float launchSpeed;
    private float minimumArcHeight;
    private float ballRadius;
    private FlightLeg currentLeg;
    public bool IsHeadingToPlayer => currentLeg == FlightLeg.ToPlayer;

    public void Begin(
        Rigidbody body,
        Collider table,
        Collider player,
        Collider netCollider,
        Vector3 landingPoint,
        Vector3 targetPoint,
        float speed,
        float arcHeight,
        float radius,
        Collider ballCollider)
    {
        ballBody = body;
        tableArea = table;
        playerHitbox = player;
        this.ballCollider = ballCollider;
        net = netCollider;
        tableLandingPoint = landingPoint;
        playerTarget = targetPoint;
        launchSpeed = speed;
        minimumArcHeight = arcHeight;
        ballRadius = radius;

        ballBody.linearVelocity = Vector3.zero;
        ballBody.angularVelocity = Vector3.zero;
        currentLeg = FlightLeg.ToTable;

        if (!TryLaunch(tableLandingPoint, launchSpeed))
        {
            currentLeg = FlightLeg.Complete;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentLeg != FlightLeg.ToTable || collision.collider != tableArea)
        {
            return;
        }

        currentLeg = FlightLeg.ToPlayer;
        float reboundSpeed = CalculateReboundSpeed(collision);
        if (reboundSpeed <= Mathf.Epsilon || !TryLaunch(playerTarget, reboundSpeed))
        {
            currentLeg = FlightLeg.Complete;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentLeg != FlightLeg.ToPlayer)
            return;

        if (other == playerHitbox ||
            other.transform.IsChildOf(playerHitbox.transform))
        {
            currentLeg = FlightLeg.Complete;
        }
    }

    private bool TryLaunch(Vector3 destination, float speed)
    {
        Vector3 gravity = Physics.gravity;
        float downwardGravity = -gravity.y;

        Vector3 start = ballBody.position;
        float horizontalDistance = Vector2.Distance(
            new Vector2(start.x, start.z),
            new Vector2(destination.x, destination.z));
        float duration = horizontalDistance / speed;
        duration = Mathf.Max(duration, Mathf.Sqrt(8.0f * minimumArcHeight / downwardGravity));
        duration = Mathf.Max(duration, GetNetClearanceDuration(start, destination, downwardGravity));
        duration = Mathf.Max(duration, 0.02f);

        ballBody.linearVelocity = (destination - start) / duration - 0.5f * gravity * duration;
        return true;
    }

    private float CalculateReboundSpeed(Collision collision)
    {
        ContactPoint contact = collision.contactCount > 0
            ? collision.GetContact(0)
            : default;
        Vector3 normal = collision.contactCount > 0 ? contact.normal : Vector3.up;
        Vector3 incomingVelocity = collision.relativeVelocity;
        if (Vector3.Dot(incomingVelocity, normal) > 0.0f)
        {
            normal = -normal;
        }

        float incomingNormalSpeed = -Vector3.Dot(incomingVelocity, normal);
        if (incomingNormalSpeed <= Mathf.Epsilon)
        {
            return 0.0f;
        }

        PhysicsMaterial ballMaterial = ballCollider.sharedMaterial;
        PhysicsMaterial tableMaterial = collision.collider.sharedMaterial;
        float restitution = CombineMaterialValue(
            ballMaterial != null ? ballMaterial.bounciness : 0.0f,
            ballMaterial != null ? ballMaterial.bounceCombine : PhysicsMaterialCombine.Average,
            tableMaterial != null ? tableMaterial.bounciness : 0.0f,
            tableMaterial != null ? tableMaterial.bounceCombine : PhysicsMaterialCombine.Average);
        float friction = CombineMaterialValue(
            ballMaterial != null ? ballMaterial.dynamicFriction : 0.6f,
            ballMaterial != null ? ballMaterial.frictionCombine : PhysicsMaterialCombine.Average,
            tableMaterial != null ? tableMaterial.dynamicFriction : 0.6f,
            tableMaterial != null ? tableMaterial.frictionCombine : PhysicsMaterialCombine.Average);

        Vector3 tangentVelocity = Vector3.ProjectOnPlane(incomingVelocity, normal);
        float frictionImpulse = friction * (1.0f + restitution) * incomingNormalSpeed;
        float tangentSpeedAfterFriction = Mathf.Max(
            0.0f,
            tangentVelocity.magnitude - frictionImpulse);
        float normalSpeedAfterBounce = restitution * incomingNormalSpeed;
        return Mathf.Sqrt(
            normalSpeedAfterBounce * normalSpeedAfterBounce +
            tangentSpeedAfterFriction * tangentSpeedAfterFriction);
    }

    private static float CombineMaterialValue(
        float first,
        PhysicsMaterialCombine firstMode,
        float second,
        PhysicsMaterialCombine secondMode)
    {
        PhysicsMaterialCombine mode = (int)firstMode >= (int)secondMode
            ? firstMode
            : secondMode;

        switch (mode)
        {
            case PhysicsMaterialCombine.Minimum:
                return Mathf.Min(first, second);
            case PhysicsMaterialCombine.Multiply:
                return first * second;
            case PhysicsMaterialCombine.Maximum:
                return Mathf.Max(first, second);
            default:
                return (first + second) * 0.5f;
        }
    }

    private float GetNetClearanceDuration(
        Vector3 start,
        Vector3 destination,
        float downwardGravity)
    {
        if (net == null)
        {
            return 0.0f;
        }

        Bounds netBounds = net.bounds;
        float duration = 0.0f;

        for (int sample = 1; sample < 100; sample++)
        {
            float t = sample / 100.0f;
            float x = Mathf.Lerp(start.x, destination.x, t);
            float z = Mathf.Lerp(start.z, destination.z, t);
            if (x < netBounds.min.x || x > netBounds.max.x ||
                z < netBounds.min.z || z > netBounds.max.z)
            {
                continue;
            }

            float clearance = netBounds.max.y + ballRadius;
            float straightHeight = Mathf.Lerp(start.y, destination.y, t);
            float additionalHeight = clearance - straightHeight;
            if (additionalHeight <= 0.0f)
            {
                continue;
            }

            float requiredDuration = Mathf.Sqrt(
                2.0f * additionalHeight / (downwardGravity * t * (1.0f - t)));
            duration = Mathf.Max(duration, requiredDuration);
        }

        return duration;
    }

    public void ResetTrajectory()
    {
        currentLeg = FlightLeg.Complete;

        if (ballBody != null)
        {
            ballBody.linearVelocity = Vector3.zero;
            ballBody.angularVelocity = Vector3.zero;
        }
    }
}