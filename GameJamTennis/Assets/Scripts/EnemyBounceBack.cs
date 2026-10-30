using UnityEngine;

public class EnemyBounceBack : MonoBehaviour
{
    public float bouncebackPercent = 100.0f;
    public GameObject playerHitbox;
    public GameObject ball;
    [Tooltip("A non-trigger collider covering the player's half of the table.")]
    public Collider playerSideTableArea;
    [Tooltip("Optional net collider; the first arc will clear its top.")]
    public Collider net;
    public float baseLaunchSpeed = 7.0f;
    public float minimumArcHeight = 1.0f;

    private bool hasReturnedBall;

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

        if (playerCollider == null || playerSideTableArea == null || ballCollider == null)
        {
            Debug.LogError(
                "EnemyBounceBack requires a player hitbox collider, ball collider, and player-side table collider.",
                this);
            return;
        }

        if (bouncebackPercent <= 0.0f || baseLaunchSpeed <= 0.0f || minimumArcHeight < 0.0f)
        {
            Debug.LogError("EnemyBounceBack has invalid launch settings.", this);
            return;
        }

        Vector3 landingPoint;
        if (!TryGetRandomTablePoint(playerSideTableArea, ballCollider, out landingPoint))
        {
            Debug.LogError("Could not find a surface point in the player-side table area.", this);
            return;
        }

        Vector3 playerTarget = GetRandomPointInside(playerCollider);
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
        trajectory.Begin(
            ballBody,
            playerSideTableArea,
            playerCollider,
            net,
            landingPoint,
            playerTarget,
            baseLaunchSpeed * bouncebackPercent / 100.0f,
            minimumArcHeight,
            ballRadius);
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

    private static bool TryGetRandomTablePoint(
        Collider tableArea,
        Collider ballCollider,
        out Vector3 point)
    {
        Bounds bounds = tableArea.bounds;
        float rayPadding = Mathf.Max(1.0f, bounds.size.y);
        float ballRadius = Mathf.Max(
            ballCollider.bounds.extents.x,
            ballCollider.bounds.extents.y,
            ballCollider.bounds.extents.z);

        for (int attempt = 0; attempt < 64; attempt++)
        {
            Vector3 origin = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.max.y + rayPadding,
                Random.Range(bounds.min.z, bounds.max.z));
            Ray ray = new Ray(origin, Vector3.down);
            RaycastHit hit;

            if (tableArea.Raycast(ray, out hit, bounds.size.y + rayPadding * 2.0f))
            {
                point = hit.point + Vector3.up * ballRadius;
                return true;
            }
        }

        point = Vector3.zero;
        return false;
    }
}
