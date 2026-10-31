
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Swinging,
        Hit
    }

    [Header("References")]
    [SerializeField] private Transform ball;
    [SerializeField] private Renderer enemyRenderer;

    [Header("Pose Materials")]
    [SerializeField] private Material idleMaterial;
    [SerializeField] private Material swingingMaterial;
    [SerializeField] private Material hitMaterial;

    [Header("Movement")]
    [SerializeField] private float followSpeed = 3f;

    [Header("Table Side Detection")]
    [SerializeField] private float tableCenterZ = 0f;
    [SerializeField] private bool enemySideIsPositiveZ = true;

    [Header("Hit Reaction")]
    [SerializeField] private float hitDuration = 0.3f;

    private EnemyState currentState = EnemyState.Idle;
    private float hitTimer;

    private void Update()
    {
        FollowBall();

        if (currentState == EnemyState.Hit)
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

        float targetX = ball.position.x;

        Vector3 position = transform.position;

        position.x = Mathf.MoveTowards(
            position.x,
            targetX,
            followSpeed * Time.deltaTime
        );

        transform.position = position;
    }

    private void UpdateSuitablePose()
    {
        if (ball == null)
            return;

        bool ballOnEnemySide;

        if (enemySideIsPositiveZ)
        {
            ballOnEnemySide = ball.position.z > tableCenterZ;
        }
        else
        {
            ballOnEnemySide = ball.position.z < tableCenterZ;
        }

        SetState(
            ballOnEnemySide
                ? EnemyState.Swinging
                : EnemyState.Idle
        );
    }

    public void OnBallHit()
    {
        hitTimer = hitDuration;
        SetState(EnemyState.Hit);
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
        }
    }
}