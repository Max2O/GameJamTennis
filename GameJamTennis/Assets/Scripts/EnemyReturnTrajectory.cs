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
    private Collider net;
    private Vector3 tableLandingPoint;
    private Vector3 playerTarget;
    private float launchSpeed;
    private float minimumArcHeight;
    private float ballRadius;
    private FlightLeg currentLeg;

    public void Begin(
        Rigidbody body,
        Collider table,
        Collider player,
        Collider netCollider,
        Vector3 landingPoint,
        Vector3 targetPoint,
        float speed,
        float arcHeight,
        float radius)
    {
        ballBody = body;
        tableArea = table;
        playerHitbox = player;
        net = netCollider;
        tableLandingPoint = landingPoint;
        playerTarget = targetPoint;
        launchSpeed = speed;
        minimumArcHeight = arcHeight;
        ballRadius = radius;

        ballBody.linearVelocity = Vector3.zero;
        ballBody.angularVelocity = Vector3.zero;
        currentLeg = FlightLeg.ToTable;

        if (!TryLaunch(tableLandingPoint))
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
        if (!TryLaunch(playerTarget))
        {
            currentLeg = FlightLeg.Complete;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentLeg == FlightLeg.ToPlayer && other == playerHitbox)
        {
            currentLeg = FlightLeg.Complete;
            ballBody.linearVelocity = Vector3.zero;
            ballBody.angularVelocity = Vector3.zero;
        }
    }

    private bool TryLaunch(Vector3 destination)
    {
        Vector3 gravity = Physics.gravity;
        float downwardGravity = -gravity.y;

        Vector3 start = ballBody.position;
        float horizontalDistance = Vector2.Distance(
            new Vector2(start.x, start.z),
            new Vector2(destination.x, destination.z));
        float duration = horizontalDistance / launchSpeed;
        duration = Mathf.Max(duration, Mathf.Sqrt(8.0f * minimumArcHeight / downwardGravity));
        duration = Mathf.Max(duration, GetNetClearanceDuration(start, destination, downwardGravity));
        duration = Mathf.Max(duration, 0.02f);

        ballBody.linearVelocity = (destination - start) / duration - 0.5f * gravity * duration;
        return true;
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
}
