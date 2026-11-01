
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerShot : MonoBehaviour
{
    [Header("Base Shot Settings")]
    [Tooltip("Ball height at which it starts receiving height-based power.")]
    [SerializeField] private float lowBallHeight = 0.15f;
    [Tooltip("Ball height at which it receives maximum height-based power.")]
    [SerializeField] private float highBallHeight = 0.8f;
    [Tooltip("Maximum horizontal distance used for distance normalization.")]
    [SerializeField] private float maximumShotDistance = 2.0f;

    [Header("Vertical Arc Heights")]
    [SerializeField] private float maximumSmashDrop = 0.5f;
    [SerializeField] private float maximumLobHeight = 1.5f;

    [Header("Shot Behaviour")]
    [Range(0f, 1f)]
    [SerializeField] private float heightArcWeight = 0.65f;
    [Range(0f, 1f)]
    [SerializeField] private float distanceArcWeight = 0.35f;

    [Header("Height Reference")]
    [SerializeField] private float tableHeight = -65f;

    [Header("Boomerang")]
    [SerializeField] private CardEffectManager cardEffectManager;
    [SerializeField] private Collider tableCollider;
    [SerializeField] private Collider playerHitbox;
    [SerializeField] private Collider netCollider;
    [SerializeField] private BallCaptureQTE ballCaptureQTE;
    [SerializeField] private float boomerangReturnSpeed = 5f;
    [SerializeField] private float boomerangReturnArcHeight = 0.5f;

    private float gravityBend;
    public float GravityBend => gravityBend;

    private Rigidbody ballBody;
    private bool ballMirrorActive;

    private int remainingBoomerangReturns;
    private bool isOutgoingPlayerShot;

    public int RemainingBoomerangReturns => remainingBoomerangReturns;

    public void SetBallMirrorActive(bool active)
    {
        ballMirrorActive = active;
    }

    private void Awake()
    {
        ballBody = GetComponent<Rigidbody>();
    }

    public bool TryCalculatePreview(
        Vector3 start,
        Vector3 target,
        out Vector3 velocity,
        out float flightTime)
    {
        return TryCalculateShot(start, target, out velocity, out flightTime);
    }

    // Starts a completely new player shot and initializes its Boomerang charges.
    public bool LaunchShot(Vector3 target)
    {
        remainingBoomerangReturns = cardEffectManager != null
            ? cardEffectManager.BoomerangStacks
            : 0;

        Debug.Log(
            "LaunchShot called. PlayerShot: " + GetInstanceID() +
            ", Card manager: " +
            (cardEffectManager != null ? cardEffectManager.GetInstanceID().ToString() : "NULL") +
            ", Boomerang stacks: " +
            (cardEffectManager != null ? cardEffectManager.BoomerangStacks.ToString() : "N/A")
        );
        return LaunchVelocityToward(target);
    }

    // Replays the previous target without resetting remaining Boomerang charges.
    public bool ReplayShot(Vector3 target)
    {
        return LaunchVelocityToward(target);
    }

    private bool LaunchVelocityToward(Vector3 target)
    {
        if (!TryCalculateShot(
            ballBody.position,
            target,
            out Vector3 velocity,
            out float flightTime))
        {
            Debug.LogWarning("Could not calculate a valid shot.", this);
            isOutgoingPlayerShot = false;
            return false;
        }

        if (ballMirrorActive)
            velocity.x = -velocity.x;

        ballBody.linearVelocity = velocity;
        ballBody.angularVelocity = Vector3.zero;

        isOutgoingPlayerShot = true;
        return true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Boomerang charges: " + remainingBoomerangReturns);
        Debug.Log("Boomerang table collider: " +
            (tableCollider != null ? tableCollider.name : "NOT ASSIGNED"));
        if (!isOutgoingPlayerShot || remainingBoomerangReturns <= 0)
            return;

        if (tableCollider == null)
            return;

        if (collision.collider != tableCollider &&
            !collision.collider.transform.IsChildOf(tableCollider.transform))
            return;

        // Consume exactly one charge for this player-shot table collision.
        remainingBoomerangReturns--;
        isOutgoingPlayerShot = false;
        Debug.Log("Boomerang activated! Remaining before consumption: " + remainingBoomerangReturns);

        EnemyReturnTrajectory trajectory =
            GetComponent<EnemyReturnTrajectory>();

        if (trajectory == null)
            trajectory = gameObject.AddComponent<EnemyReturnTrajectory>();

        float ballRadius = GetComponent<Collider>() != null
            ? GetComponent<Collider>().bounds.extents.magnitude
            : 0.1f;

        trajectory.BeginBoomerangReturn(
            ballBody,
            playerHitbox,
            netCollider,
            boomerangReturnSpeed,
            boomerangReturnArcHeight,
            ballRadius,
            ballCaptureQTE
        );
    }

    private void FixedUpdate()
    {
        if (Mathf.Abs(gravityBend) < 0.001f)
            return;

        if (ballBody.position.y <= tableHeight + 0.05f)
            return;

        ballBody.AddForce(
            Vector3.right * gravityBend,
            ForceMode.Acceleration
        );
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

        float distance = Vector2.Distance(
            new Vector2(start.x, start.z),
            new Vector2(target.x, target.z));

        float ballHeight = start.y - tableHeight;

        float heightFactor = Mathf.InverseLerp(
            lowBallHeight, highBallHeight, ballHeight);

        float distanceFactor = Mathf.InverseLerp(
            0.25f, maximumShotDistance, distance);

        float arcFactor =
            (1f - heightFactor) *
            (heightArcWeight + distanceFactor * distanceArcWeight);

        arcFactor = Mathf.Clamp01(arcFactor);

        float desiredPeakHeight = Mathf.Lerp(
            -maximumSmashDrop, maximumLobHeight, arcFactor);

        float verticalVelocity = desiredPeakHeight > 0f
            ? Mathf.Sqrt(2f * gravity * desiredPeakHeight)
            : -Mathf.Sqrt(2f * gravity * -desiredPeakHeight);

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

        Vector3 horizontalDifference = new Vector3(
            target.x - start.x, 0f, target.z - start.z);

        velocity = horizontalDifference / flightTime +
                   Vector3.up * verticalVelocity;

        return true;
    }

    public Vector3 EvaluateTrajectory(
        Vector3 start,
        Vector3 initialVelocity,
        float elapsedTime)
    {
        return start + initialVelocity * elapsedTime +
               0.5f * Physics.gravity * elapsedTime * elapsedTime;
    }

    public void SetGravityBend(float strength)
    {
        gravityBend = strength;
    }
}