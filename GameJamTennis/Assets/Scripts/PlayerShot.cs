
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerShot : MonoBehaviour
{
    [Header("Base Shot Settings")]

    [Tooltip("Ball height at which it starts receiving height-based power.")]
    [SerializeField] private float lowBallHeight = 0.15f;

    [Tooltip("Ball height at which it receives maximum height-based power.")]
    [SerializeField] private float highBallHeight = 1.2f;

    [Tooltip("Maximum horizontal distance used for distance normalization.")]
    [SerializeField] private float maximumShotDistance = 2.0f;

    [Header("Vertical Shot Speeds")]

    [Tooltip("Downward launch speed for a high, close shot.")]
    [SerializeField] private float maximumDownwardSpeed = 3.5f;

    [Tooltip("Upward launch speed for a low, distant shot.")]
    [SerializeField] private float maximumLobSpeed = 5.0f;

    [Header("Shot Behaviour")]

    [Range(0f, 1f)]
    [SerializeField] private float heightArcWeight = 0.65f;

    [Range(0f, 1f)]
    [SerializeField] private float distanceArcWeight = 0.35f;

    private Rigidbody ballBody;

    private void Awake()
    {
        ballBody = GetComponent<Rigidbody>();
    }

    // Calculates the base-stat shot used by the trajectory preview.
    public bool TryCalculatePreview(
        Vector3 start,
        Vector3 target,
        out Vector3 velocity,
        out float flightTime)
    {
        return TryCalculateShot(
            start,
            target,
            out velocity,
            out flightTime
        );
    }

    // Calculates and launches the actual shot.
    public bool LaunchShot(Vector3 target)
    {
        if (!TryCalculateShot(
            ballBody.position,
            target,
            out Vector3 velocity,
            out float flightTime))
        {
            Debug.LogWarning("Could not calculate a valid shot.", this);
            return false;
        }

        ballBody.linearVelocity = velocity;
        ballBody.angularVelocity = Vector3.zero;

        return true;
    }

    private bool TryCalculateShot(
        Vector3 start,
        Vector3 target,
        out Vector3 velocity,
        out float flightTime)
    {
        velocity = Vector3.zero;
        flightTime = 0f;

        float gravity = -Physics.gravity.y;

        if (gravity <= 0.001f)
            return false;

        // Horizontal distance across the table.
        float distance = Vector2.Distance(
            new Vector2(start.x, start.z),
            new Vector2(target.x, target.z)
        );

        // How high the ball is above its intended landing position.
        float ballHeight = start.y - target.y;

        // Normalize the incoming ball height.
        float heightFactor = Mathf.InverseLerp(
            lowBallHeight,
            highBallHeight,
            ballHeight
        );

        // Normalize the shot distance.
        float distanceFactor = Mathf.InverseLerp(
            0.25f,
            maximumShotDistance,
            distance
        );

        // Low balls and long shots produce more arched trajectories.
        float arcFactor =
            (1f - heightFactor) * heightArcWeight +
            distanceFactor * distanceArcWeight;

        arcFactor = Mathf.Clamp01(arcFactor);

        // Interpolate between a downward smash and an upward lob.
        float verticalVelocity = Mathf.Lerp(
            -maximumDownwardSpeed,
            maximumLobSpeed,
            arcFactor
        );

        // Solve the ballistic equation for the descending arrival time.
        float verticalDifference = target.y - start.y;

        float discriminant =
            verticalVelocity * verticalVelocity -
            2f * gravity * verticalDifference;

        if (discriminant < 0f)
            return false;

        flightTime = (
            verticalVelocity + Mathf.Sqrt(discriminant)
        ) / gravity;

        if (flightTime <= 0.001f)
            return false;

        // Calculate the horizontal velocity needed to reach the target.
        Vector3 horizontalDifference = new Vector3(
            target.x - start.x,
            0f,
            target.z - start.z
        );

        Vector3 horizontalVelocity =
            horizontalDifference / flightTime;

        velocity = horizontalVelocity +
                   Vector3.up * verticalVelocity;

        return true;
    }

    // Evaluates the same projectile equation used by the Rigidbody.
    public Vector3 EvaluateTrajectory(
        Vector3 start,
        Vector3 initialVelocity,
        float elapsedTime)
    {
        return start +
               initialVelocity * elapsedTime +
               0.5f * Physics.gravity *
               elapsedTime * elapsedTime;
    }
}