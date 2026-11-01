using UnityEngine;

public class EnemyBounceBack : MonoBehaviour
{
    public float bouncebackPercent = 100.0f;
    public GameObject playerHitbox;
    public BallCaptureQTE ballCaptureQTE;
    public GameObject ball;
    [Tooltip("Net collider; the first arc will clear its top.")]
    public Collider net;
    public float baseLaunchSpeed = 1.0f;
    public float minimumArcHeight = 0.1f;

    private bool hasReturnedBall;

    [Header("Table Colliders")]
    [SerializeField] private Collider tableSurface;
    [SerializeField] private Collider playerSideTableArea;

    private void OnTriggerEnter(Collider other)
    {
        if (hasReturnedBall || ball == null || other.attachedRigidbody == null)
        {
            return;
        }

        Rigidbody ballBody = ball.GetComponent<Rigidbody>();
        if (ballBody == null || other.attachedRigidbody != ballBody)
        {
            return;
        }

        Collider playerCollider = playerHitbox != null
            ? playerHitbox.GetComponentInChildren<Collider>()
            : null;
        Collider ballCollider = ball.GetComponent<Collider>();

        if (bouncebackPercent <= 0.0f || baseLaunchSpeed <= 0.0f || minimumArcHeight < 0.0f)
        {
            Debug.LogError("EnemyBounceBack has invalid launch settings.", this);
            return;
        }

        Vector3 playerTarget = GetRandomPointInside(playerCollider);
        Vector3 ballStart = ballBody.position;
        Vector3 landingPoint;
        if (!TryGetRandomBouncePoint(
                playerSideTableArea,
                ballCollider,
                ballStart,
                playerTarget,
                out landingPoint))
        {
            Debug.LogError(
                "Could not find a bounce point where the vertical plane from the ball to the player target intersects the player-side table.",
                this);
            return;
        }

        float ballRadius = Mathf.Max(
            ballCollider.bounds.extents.x,
            ballCollider.bounds.extents.y,
            ballCollider.bounds.extents.z);

        EnemyReturnTrajectory trajectory = ball.GetComponent<EnemyReturnTrajectory>();
        if (trajectory == null)
        {
            trajectory = ball.AddComponent<EnemyReturnTrajectory>();
        }

        hasReturnedBall = true;
        ballCaptureQTE.shouldBegin = true;
        trajectory.Begin(
            ballBody,
            tableSurface,
            playerCollider,
            net,
            landingPoint,
            playerTarget,
            baseLaunchSpeed * bouncebackPercent / 100.0f,
            minimumArcHeight,
            ballRadius,
            ballCollider);
        ball.GetComponent<NetFault>().BeginEnemyShot();
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody ballBody = ball != null ? ball.GetComponent<Rigidbody>() : null;
        if (ballBody != null && other.attachedRigidbody == ballBody)
        {
            hasReturnedBall = false;
        }
    }

    private static Vector3 GetRandomPointInside(Collider target)
    {
        Bounds bounds = target.bounds;
        for (int attempt = 0; attempt < 64; attempt++)
        {
            Vector3 point = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z));

            if ((target.ClosestPoint(point) - point).sqrMagnitude < 0.000001f)
            {
                return point;
            }
        }

        return target.ClosestPoint(bounds.center);
    }

    private static bool TryGetRandomBouncePoint(
        Collider tableArea,
        Collider ballCollider,
        Vector3 lineStart,
        Vector3 lineEnd,
        out Vector3 point)
    {
        Bounds bounds = tableArea.bounds;
        Vector2 start = new Vector2(lineStart.x, lineStart.z);
        Vector2 end = new Vector2(lineEnd.x, lineEnd.z);
        Vector2 direction = end - start;
        Vector2 planeNormal = direction.sqrMagnitude > 0.000001f
            ? new Vector2(direction.y, -direction.x).normalized
            : Vector2.right;
        float planeOffset = Vector2.Dot(planeNormal, start);
        Vector2 min = new Vector2(bounds.min.x, bounds.min.z);
        Vector2 max = new Vector2(bounds.max.x, bounds.max.z);
        Vector2[] corners =
        {
            new Vector2(min.x, min.y),
            new Vector2(max.x, min.y),
            new Vector2(max.x, max.y),
            new Vector2(min.x, max.y)
        };
        Vector2[] intersections = new Vector2[8];
        int intersectionCount = 0;
        float longestIntersection = 0.0f;
        Vector2 intersectionStart = Vector2.zero;
        Vector2 intersectionEnd = Vector2.zero;
        const float planeTolerance = 0.0001f;

        for (int edge = 0; edge < corners.Length; edge++)
        {
            Vector2 a = corners[edge];
            Vector2 b = corners[(edge + 1) % corners.Length];
            float distanceA = Vector2.Dot(planeNormal, a) - planeOffset;
            float distanceB = Vector2.Dot(planeNormal, b) - planeOffset;

            if (Mathf.Abs(distanceA) <= planeTolerance &&
                Mathf.Abs(distanceB) <= planeTolerance)
            {
                AddIntersection(a, intersections, ref intersectionCount, planeTolerance);
                AddIntersection(b, intersections, ref intersectionCount, planeTolerance);
            }
            else if (Mathf.Abs(distanceA) <= planeTolerance)
            {
                AddIntersection(a, intersections, ref intersectionCount, planeTolerance);
            }
            else if (Mathf.Abs(distanceB) <= planeTolerance)
            {
                AddIntersection(b, intersections, ref intersectionCount, planeTolerance);
            }
            else if ((distanceA < 0.0f) != (distanceB < 0.0f))
            {
                Vector2 crossing = Vector2.Lerp(a, b, distanceA / (distanceA - distanceB));
                AddIntersection(crossing, intersections, ref intersectionCount, planeTolerance);
            }
        }

        for (int i = 0; i < intersectionCount; i++)
        {
            for (int j = i + 1; j < intersectionCount; j++)
            {
                float length = Vector2.Distance(intersections[i], intersections[j]);
                if (length > longestIntersection)
                {
                    intersectionStart = intersections[i];
                    intersectionEnd = intersections[j];
                    longestIntersection = length;
                }
            }
        }

        if (longestIntersection <= planeTolerance)
        {
            point = Vector3.zero;
            return false;
        }

        float rayPadding = Mathf.Max(1.0f, bounds.size.y);
        float ballRadius = Mathf.Max(
            ballCollider.bounds.extents.x,
            ballCollider.bounds.extents.y,
            ballCollider.bounds.extents.z);

        for (int attempt = 0; attempt < 32; attempt++)
        {
            Vector2 sample = Vector2.Lerp(
                intersectionStart,
                intersectionEnd,
                Random.value);
            Vector3 origin = new Vector3(
                sample.x,
                bounds.max.y + rayPadding,
                sample.y);
            Ray ray = new Ray(origin, Vector3.down);
            RaycastHit hit;

            if (tableArea.Raycast(ray, out hit, bounds.size.y + rayPadding * 2.0f))
            {
                point = hit.point + hit.normal * ballRadius;
                return true;
            }
        }

        point = Vector3.zero;
        return false;
    }

    private static void AddIntersection(
        Vector2 point,
        Vector2[] intersections,
        ref int intersectionCount,
        float tolerance)
    {
        for (int i = 0; i < intersectionCount; i++)
        {
            if (Vector2.Distance(intersections[i], point) <= tolerance)
            {
                return;
            }
        }

        intersections[intersectionCount++] = point;
    }

    public void ResetRoundState()
    {
        hasReturnedBall = false;
    }
}