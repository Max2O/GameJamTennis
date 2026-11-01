
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Swinging,
        Hit,
        Crouching,
        CrouchingHit
    }

    [Header("References")]
    [SerializeField] private Transform ball;
    [SerializeField] private Renderer enemyRenderer;

    [Header("Normal Pose Materials")]
    [SerializeField] private Material idleMaterial;
    [SerializeField] private Material swingingMaterial;
    [SerializeField] private Material hitMaterial;

    [Header("Crouching Pose Materials")]
    [SerializeField] private Material crouchingSwingingMaterial;
    [SerializeField] private Material crouchingHitMaterial;

    [Header("Movement")]
    [SerializeField] private float followSpeed = 3f;

    [Header("Table Side Detection")]
    [SerializeField] private float tableCenterZ = 0f;
    [SerializeField] private bool enemySideIsPositiveZ = true;

    [Header("Hit Reaction")]
    [SerializeField] private float hitDuration = 0.3f;

    private EnemyState currentState = EnemyState.Idle;

    private float hitTimer;
    private float originalZ;

    private bool lowZoneActive;

    private void Awake()
    {
        originalZ = transform.position.z;
    }

    private void Update()
    {
        FollowBall();

        if (currentState == EnemyState.Hit ||
            currentState == EnemyState.CrouchingHit)
        {
            hitTimer -= Time.deltaTime;

            if (hitTimer <= 0f)
            {
                UpdateSuitablePose();
            }

            return;
        }

        UpdateSuitablePose();
    }

    private void FollowBall()
    {
        if (ball == null)
            return;

        Vector3 position = transform.position;

        // Always follow the ball along X.
        position.x = Mathf.MoveTowards(
            position.x,
            ball.position.x,
            followSpeed * Time.deltaTime
        );

        // Follow along Z only while crouching.
        float targetZ = lowZoneActive
            ? ball.position.z
            : originalZ;

        position.z = Mathf.MoveTowards(
            position.z,
            targetZ,
            followSpeed * Time.deltaTime
        );

        transform.position = position;
    }

    private void UpdateSuitablePose()
    {
        if (lowZoneActive)
        {
            SetState(EnemyState.Crouching);
            return;
        }

        if (ball == null)
            return;

        bool ballOnEnemySide = enemySideIsPositiveZ
            ? ball.position.z > tableCenterZ
            : ball.position.z < tableCenterZ;

        SetState(
            ballOnEnemySide
                ? EnemyState.Swinging
                : EnemyState.Idle
        );
    }

    public void SetLowZoneActive(bool active)
    {
        lowZoneActive = active;
        UpdateSuitablePose();
    }

    public void OnBallHit(bool lowHit)
    {
        hitTimer = hitDuration;

        if (lowHit)
        {
            SetState(EnemyState.CrouchingHit);
        }
        else
        {
            SetState(EnemyState.Hit);
        }
    }

    private void SetState(EnemyState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        switch (currentState)
        {
            case EnemyState.Idle:
                enemyRenderer.sharedMaterial = idleMaterial;
                break;

            case EnemyState.Swinging:
                enemyRenderer.sharedMaterial = swingingMaterial;
                break;

            case EnemyState.Hit:
                enemyRenderer.sharedMaterial = hitMaterial;
                break;

            case EnemyState.Crouching:
                enemyRenderer.sharedMaterial = crouchingSwingingMaterial;
                break;

            case EnemyState.CrouchingHit:
                enemyRenderer.sharedMaterial = crouchingHitMaterial;
                break;
        }
    }
}
